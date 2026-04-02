using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using BattleServer.Models;

namespace BattleServer;

/// <summary>Хранилище боёв в памяти. Одна глобальная комната для быстрого теста: первые два join попадают в один бой.</summary>
public partial class BattleRoomStore
{
    private readonly object _lock = new();
    private readonly BattleHistoryDatabase _battleHistoryDb;
    private readonly BattleTurnDatabase _battleTurnDb;
    private readonly BattleWeaponDatabase _weaponDb;
    private readonly BattleMedicineDatabase _medicineDb;
    private readonly BattleObstacleBalanceDatabase _obstacleDb;
    private readonly BattleZoneShrinkDatabase _zoneShrinkDb;
    private readonly BattleBodyPartDatabase _bodyPartDb;
    private readonly BattleUserDatabase _userDb;
    private readonly BattleMapSettingsDatabase _mapSettings;
    /// <summary>Очередь ожидающих (один игрок). Как только второй присоединился — создаём бой из двух.</summary>
    private string? _waitingBattleId;

    private readonly Dictionary<string, BattleRoom> _rooms = new();

    /// <summary>
    /// WebSocket обрабатывает сообщения последовательно: синхронный CloseRound после submitAck блокировал следующий ReceiveAsync,
    /// пока клиент уже получил roundResolved из фона — следующий submitTurn не читался. CloseRound ставим в очередь TP;
    /// перед Apply ждём хвост цепочки, чтобы не применить ход до закрытия предыдущего раунда.
    /// </summary>
    private readonly ConcurrentDictionary<string, object> _battleCloseChainLocks = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Task> _battleCloseTailTasks = new(StringComparer.Ordinal);
    /// <summary>Количество незавершённых CloseRound задач в цепочке для каждого боя (только для логирования).</summary>
    private readonly ConcurrentDictionary<string, int> _battleCloseChainDepth = new(StringComparer.Ordinal);

    public BattleRoomStore(BattleHistoryDatabase battleHistoryDb, BattleTurnDatabase battleTurnDb, BattleWeaponDatabase weaponDb, BattleMedicineDatabase medicineDb, BattleObstacleBalanceDatabase obstacleDb, BattleZoneShrinkDatabase zoneShrinkDb, BattleBodyPartDatabase bodyPartDb, BattleUserDatabase userDb, BattleMapSettingsDatabase mapSettings)
    {
        _battleHistoryDb = battleHistoryDb;
        _battleTurnDb = battleTurnDb;
        _weaponDb = weaponDb;
        _medicineDb = medicineDb;
        _obstacleDb = obstacleDb;
        _zoneShrinkDb = zoneShrinkDb;
        _bodyPartDb = bodyPartDb;
        _userDb = userDb;
        _mapSettings = mapSettings;
    }

    /// <summary>Таймер раундов (вызывать из фона).</summary>
    public void Tick(float deltaSeconds)
    {
        lock (_lock)
        {
            foreach (var r in _rooms.Values)
                r.Tick(deltaSeconds);
        }
    }

    /// <summary>Присоединиться к поиску. Возвращает (battleId, playerId, battleStarted?, room). Если battleStarted != null — бой начался (2 игрока).</summary>
    public (string battleId, string playerId, BattleRoom room, BattleStartedPayloadDto? battleStarted) Join(int startCol = 0, int startRow = 0)
    {
        lock (_lock)
        {
            var (gw, gh) = _mapSettings.GetMapDimensions();
            if (_waitingBattleId == null)
            {
                var bid = Guid.NewGuid().ToString("N")[..8];
                _waitingBattleId = bid;
                var room = new BattleRoom(bid, gw, gh, _weaponDb, _obstacleDb, _bodyPartDb, _userDb, _zoneShrinkDb, _medicineDb);
                var sp1 = HexSpawn.FindTwoTeamSpawnsOnOppositeHorizontalSides(1, gw, gh);
                if (sp1.Count < 1)
                    throw new InvalidOperationException("PVP 1v1 spawn failed");
                int p1c = sp1[0].col;
                int p1r = sp1[0].row;
                room.AddPlayer("P1", p1c, p1r);
                room.SetPlayerDisplayInfo("P1", "P1", 1);
                _rooms[bid] = room;
                _battleHistoryDb.EnsureBattle(bid);
                return (bid, "P1", room, null);
            }

            var existingRoom = _rooms[_waitingBattleId];
            var sp = HexSpawn.FindTwoTeamSpawnsOnOppositeHorizontalSides(1, gw, gh);
            if (sp.Count < 2)
                throw new InvalidOperationException("PVP 1v1 spawn failed");
            existingRoom.SetPlayerSpawnPosition("P1", sp[0].col, sp[0].row);
            existingRoom.AddPlayer("P2", sp[1].col, sp[1].row);
            existingRoom.SetPlayerDisplayInfo("P2", "P2", 1);
            existingRoom.MatchModeWire = "1v1";
            existingRoom.StartFirstRound();
            var id = _waitingBattleId;
            _waitingBattleId = null;
            return (id!, "P2", existingRoom, existingRoom.BuildBattleStartedFor("P2"));
        }
    }

    /// <summary>Второй игрок — вызываем после того как первый уже в очереди. Передаём battleId от первого (для теста можно один общий).</summary>
    public (string battleId, string playerId, BattleRoom room, BattleStartedPayloadDto? battleStarted) JoinSecond(string battleId, int startCol = 24, int startRow = 39)
    {
        lock (_lock)
        {
            if (!_rooms.TryGetValue(battleId, out var room) || room.Players.Count != 1)
                throw new InvalidOperationException("No waiting room for this battleId");

            var (gw, gh) = (room.MapWidth, room.MapHeight);
            var spJv = HexSpawn.FindTwoTeamSpawnsOnOppositeHorizontalSides(1, gw, gh);
            if (spJv.Count < 2)
                throw new InvalidOperationException("PVP 1v1 spawn failed");
            room.SetPlayerSpawnPosition("P1", spJv[0].col, spJv[0].row);
            room.AddPlayer("P2", spJv[1].col, spJv[1].row);
            room.SetPlayerDisplayInfo("P2", "P2", 1);
            room.MatchModeWire = "1v1";
            room.StartFirstRound();
            var started = room.BuildBattleStartedFor("P2");
            _waitingBattleId = null;
            return (battleId, "P2", room, started);
        }
    }

    /// <summary>
    /// Упрощённый join.
    /// - Если solo == true — создаём одиночный бой (1 игрок + серверный моб), сразу запускаем раунд и возвращаем battleStarted.
    /// - Если есть ожидающий и solo == false — создаём пару и возвращаем battleStarted второму игроку.
    /// - Иначе встаём в очередь как P1 и ждём второго игрока.
    /// </summary>
    public JoinResponse JoinOrCreate(int startCol, int startRow, bool solo, int playerMaxHp, int playerCurrentHp, int playerMaxAp, long weaponItemId, int weaponDamageMin, int weaponDamageMax, int weaponRange, int weaponAttackApCost, string displayName, long battleUserId, int characterLevel = 1, int accuracy = 10, double weaponTightness = 1, int weaponTrajectoryHeight = 1, bool weaponIsSniper = false)
    {
        lock (_lock)
        {
            bool haveBattleUserId = battleUserId > 0;

            // Одиночный бой: не используем очередь, сразу создаём комнату и запускаем первый раунд.
            if (solo)
            {
                var (gw, gh) = _mapSettings.GetMapDimensions();
                var soloBattleId = Guid.NewGuid().ToString("N")[..8];
                var soloRoom = new BattleRoom(soloBattleId, gw, gh, _weaponDb, _obstacleDb, _bodyPartDb, _userDb, _zoneShrinkDb, _medicineDb)
                {
                    IsSolo = true,
                    MatchModeWire = "solo"
                };
                int soloCol = Math.Clamp(startCol, 0, gw - 1);
                int soloRow = Math.Clamp(startRow, 0, gh - 1);
                soloRoom.AddPlayer("P1", soloCol, soloRow);
                soloRoom.SetPlayerDisplayInfo("P1", displayName, characterLevel);
                if (haveBattleUserId)
                {
                    soloRoom.RegisterBattlePlayerUserId("P1", battleUserId);
                    if (_userDb.TryGetUserProgressProfileByUserId(battleUserId, out var soloProf))
                        soloRoom.SetPlayerUnitCardCombatStats("P1", soloProf.Strength, soloProf.Agility, soloProf.Intuition, soloProf.Endurance, soloProf.Accuracy, soloProf.Intellect);
                }
                soloRoom.SetPlayerCombatProfile("P1", playerMaxHp, playerMaxAp, weaponItemId, weaponDamageMin, weaponDamageMax, weaponRange, weaponAttackApCost, accuracy, weaponTightness, weaponTrajectoryHeight, weaponIsSniper);
                soloRoom.SetPlayerCurrentHpOverride("P1", playerCurrentHp);
                _rooms[soloBattleId] = soloRoom;
                _battleHistoryDb.EnsureBattle(soloBattleId);
                soloRoom.StartFirstRound();
                Console.WriteLine($"[tzInfo] Solo join: battleId={soloBattleId}, playerId=P1, start=({soloCol},{soloRow})");
                return new JoinResponse
                {
                    BattleId = soloBattleId,
                    PlayerId = "P1",
                    Status = "battle",
                    BattleStarted = soloRoom.BuildBattleStartedFor("P1")
                };
            }

            if (_waitingBattleId != null && _rooms.TryGetValue(_waitingBattleId, out var waitingRoom))
            {
                var battleId = _waitingBattleId;
                var (gw2, gh2) = (waitingRoom.MapWidth, waitingRoom.MapHeight);
                var spPair = HexSpawn.FindTwoTeamSpawnsOnOppositeHorizontalSides(1, gw2, gh2);
                if (spPair.Count < 2)
                    throw new InvalidOperationException("PVP 1v1 spawn failed");
                waitingRoom.SetPlayerSpawnPosition("P1", spPair[0].col, spPair[0].row);
                waitingRoom.AddPlayer("P2", spPair[1].col, spPair[1].row);
                waitingRoom.SetPlayerDisplayInfo("P2", displayName, characterLevel);
                if (haveBattleUserId)
                {
                    waitingRoom.RegisterBattlePlayerUserId("P2", battleUserId);
                    if (_userDb.TryGetUserProgressProfileByUserId(battleUserId, out var p2Prof))
                        waitingRoom.SetPlayerUnitCardCombatStats("P2", p2Prof.Strength, p2Prof.Agility, p2Prof.Intuition, p2Prof.Endurance, p2Prof.Accuracy, p2Prof.Intellect);
                }
                waitingRoom.SetPlayerCombatProfile("P2", playerMaxHp, playerMaxAp, weaponItemId, weaponDamageMin, weaponDamageMax, weaponRange, weaponAttackApCost, accuracy, weaponTightness, weaponTrajectoryHeight, weaponIsSniper);
                waitingRoom.SetPlayerCurrentHpOverride("P2", playerCurrentHp);
                waitingRoom.MatchModeWire = "1v1";
                waitingRoom.StartFirstRound();
                var p1Pos = waitingRoom.Players["P1"];
                Console.WriteLine($"[tzInfo] Matchmaking pair completed: battleId={battleId}, P1=({p1Pos.col},{p1Pos.row}), P2=({spPair[1].col},{spPair[1].row})");
                _waitingBattleId = null;
                return new JoinResponse { BattleId = battleId, PlayerId = "P2", Status = "battle", BattleStarted = waitingRoom.BuildBattleStartedFor("P2") };
            }

            var bid = Guid.NewGuid().ToString("N")[..8];
            var (gw3, gh3) = _mapSettings.GetMapDimensions();
            var r = new BattleRoom(bid, gw3, gh3, _weaponDb, _obstacleDb, _bodyPartDb, _userDb, _zoneShrinkDb, _medicineDb);
            var spWait = HexSpawn.FindTwoTeamSpawnsOnOppositeHorizontalSides(1, gw3, gh3);
            if (spWait.Count < 1)
                throw new InvalidOperationException("PVP 1v1 spawn failed");
            int pc = spWait[0].col;
            int pr = spWait[0].row;
            r.AddPlayer("P1", pc, pr);
            r.SetPlayerDisplayInfo("P1", displayName, characterLevel);
            if (haveBattleUserId)
            {
                r.RegisterBattlePlayerUserId("P1", battleUserId);
                if (_userDb.TryGetUserProgressProfileByUserId(battleUserId, out var p1Prof))
                    r.SetPlayerUnitCardCombatStats("P1", p1Prof.Strength, p1Prof.Agility, p1Prof.Intuition, p1Prof.Endurance, p1Prof.Accuracy, p1Prof.Intellect);
            }
            r.SetPlayerCombatProfile("P1", playerMaxHp, playerMaxAp, weaponItemId, weaponDamageMin, weaponDamageMax, weaponRange, weaponAttackApCost, accuracy, weaponTightness, weaponTrajectoryHeight, weaponIsSniper);
            r.SetPlayerCurrentHpOverride("P1", playerCurrentHp);
            _rooms[bid] = r;
            _battleHistoryDb.EnsureBattle(bid);
            _waitingBattleId = bid;
            Console.WriteLine($"[tzInfo] Matchmaking waiting: battleId={bid}, playerId=P1, start=({pc},{pr})");
            return new JoinResponse { BattleId = bid, PlayerId = "P1", Status = "waiting" };
        }
    }

    public BattleRoom? GetRoom(string battleId)
    {
        lock (_lock) return _rooms.TryGetValue(battleId, out var r) ? r : null;
    }

    /// <summary>Find an in-memory battle where this <c>users.id</c> is registered (queue or active fight).</summary>
    public bool TryFindActiveBattleForUser(long userId, out string battleId, out string playerId)
    {
        battleId = "";
        playerId = "";
        if (userId <= 0)
            return false;
        lock (_lock)
        {
            foreach (var kv in _rooms)
            {
                if (!kv.Value.IsUnfinishedForLoginResume())
                    continue;
                foreach (var p in kv.Value.PlayerToUserId)
                {
                    if (p.Value != userId)
                        continue;
                    battleId = kv.Key;
                    playerId = p.Key;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Записать ход под lock (коротко). Если <paramref name="mustCloseRoundAfterAck"/> true, вызовите
    /// <see cref="SubmitTurnCloseRoundLocked"/> после отправки клиенту submitAck — иначе CloseRound держит lock
    /// на время всей симуляции и клиент ловит таймаут.
    /// </summary>
    public SubmitTurnResult SubmitTurnApplyLocked(string battleId, SubmitTurnPayloadDto payload, out bool mustCloseRoundAfterAck)
    {
        mustCloseRoundAfterAck = false;
        lock (_lock)
        {
            if (!_rooms.TryGetValue(battleId, out var room))
            {
                Console.WriteLine($"[BattleStore] SubmitTurnApplyLocked reject: reason=not_found battleId={battleId}");
                return SubmitTurnResult.NotFoundRoom();
            }

            if (string.IsNullOrEmpty(payload.PlayerId) || !room.Players.ContainsKey(payload.PlayerId))
            {
                Console.WriteLine(
                    $"[BattleStore] SubmitTurnApplyLocked reject: reason=bad_player battleId={battleId} playerId={payload.PlayerId ?? "(null)"}");
                return SubmitTurnResult.BadPlayer();
            }

            if (payload.RoundIndex != room.RoundIndex)
            {
                Console.WriteLine(
                    $"[BattleStore] SubmitTurnApplyLocked reject: reason=round_mismatch battleId={battleId} playerId={payload.PlayerId} clientRound={payload.RoundIndex} serverRound={room.RoundIndex}");
                return SubmitTurnResult.RoundMismatch(room.RoundIndex);
            }

            bool shouldClose = room.SubmitTurn(payload);
            Console.WriteLine($"[tzInfo] SubmitTurn: battleId={battleId}, playerId={payload.PlayerId}, round={payload.RoundIndex}, actionCount={(payload.Actions?.Length ?? 0)}, shouldClose={shouldClose}");
            if (!shouldClose)
                return SubmitTurnResult.Accepted(false);

            mustCloseRoundAfterAck = true;
            return SubmitTurnResult.Accepted(false);
        }
    }

    /// <summary>Закрыть раунд после submitAck (см. <see cref="SubmitTurnApplyLocked"/>). Идемпотентно, если таймер уже закрыл раунд.</summary>
    public void SubmitTurnCloseRoundLocked(string battleId)
    {
        lock (_lock)
        {
            if (!_rooms.TryGetValue(battleId, out var room))
                return;
            if (room.Players.Count == 0)
                return;
            if (room.Submissions.Count < room.Players.Count)
                return;
            var sw = Stopwatch.StartNew();
            Console.WriteLine($"[BattleStore] SubmitTurnCloseRoundLocked START: battleId={battleId} round={room.RoundIndex} utc={DateTime.UtcNow:O}");
            room.CloseRound(fromTimer: false);
            sw.Stop();
            Console.WriteLine($"[BattleStore] SubmitTurnCloseRoundLocked END: battleId={battleId} elapsed={sw.ElapsedMilliseconds}ms");
        }
    }

    /// <summary>Дождаться фоновых CloseRound, запланированных с WebSocket (иначе Apply может обогнать чужой Close).</summary>
    public Task WaitBattleCloseChainAsync(string battleId)
    {
        if (string.IsNullOrEmpty(battleId))
            return Task.CompletedTask;
        var sync = _battleCloseChainLocks.GetOrAdd(battleId, _ => new object());
        Task t;
        bool found;
        lock (sync)
        {
            found = _battleCloseTailTasks.TryGetValue(battleId, out t!);
        }

        // Never log under lock(sync): Console can block on BattleLogConsoleWriter and invert with another thread holding the writer while waiting on sync.
        if (!found)
        {
            Console.WriteLine($"[BattleStore] WaitCloseChain: battleId={battleId} noChain (already done)");
            return Task.CompletedTask;
        }

        bool isPending = !t.IsCompleted;
        Console.WriteLine($"[BattleStore] WaitCloseChain: battleId={battleId} tailStatus={t.Status} isPending={isPending} depth={_battleCloseChainDepth.GetValueOrDefault(battleId)}");
        return t;
    }

    /// <summary>Закрыть раунд в фоне; не блокировать ReceiveAsync следующего submitTurn.</summary>
    public void ScheduleSubmitTurnCloseRound(string battleId)
    {
        if (string.IsNullOrEmpty(battleId))
            return;
        var sync = _battleCloseChainLocks.GetOrAdd(battleId, _ => new object());
        int depth;
        lock (sync)
        {
            var prev = _battleCloseTailTasks.TryGetValue(battleId, out var p) ? p : Task.CompletedTask;
            depth = _battleCloseChainDepth.AddOrUpdate(battleId, 1, (_, old) => old + 1);
            var next = prev.ContinueWith(_ =>
                {
                    try
                    {
                        Console.WriteLine($"[BattleStore] CloseRoundTask START: battleId={battleId} utc={DateTime.UtcNow:O}");
                        var sw = Stopwatch.StartNew();
                        SubmitTurnCloseRoundLocked(battleId);
                        sw.Stop();
                        Console.WriteLine($"[BattleStore] CloseRoundTask END: battleId={battleId} elapsed={sw.ElapsedMilliseconds}ms");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[BattleStore] ScheduleSubmitTurnCloseRound failed battleId={battleId}, err={ex}");
                    }
                    finally
                    {
                        _battleCloseChainDepth.AddOrUpdate(battleId, 0, (_, old) => Math.Max(0, old - 1));
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.RunContinuationsAsynchronously,
                TaskScheduler.Default);
            _battleCloseTailTasks[battleId] = next;
        }

        Console.WriteLine($"[BattleStore] ScheduleCloseRound: battleId={battleId} chainDepth={depth} utc={DateTime.UtcNow:O}");
    }

    /// <summary>Атомарный submit+close (без раннего ack). Для WebSocket используйте Wait + Apply + ack + Schedule.</summary>
    public SubmitTurnResult SubmitTurnLocked(string battleId, SubmitTurnPayloadDto payload)
    {
        WaitBattleCloseChainAsync(battleId).GetAwaiter().GetResult();
        var r = SubmitTurnApplyLocked(battleId, payload, out var mustClose);
        if (mustClose)
            SubmitTurnCloseRoundLocked(battleId);
        return mustClose ? SubmitTurnResult.Accepted(true) : r;
    }

    /// <summary>Игрок покинул клиент: отмена ожидания в очереди или завершение боя (таймер не тикает для удалённой комнаты).</summary>
    public bool PlayerLeft(string battleId, string playerId)
    {
        lock (_lock)
        {
            if (!_rooms.TryGetValue(battleId, out var room)) return false;
            if (!room.Players.ContainsKey(playerId)) return false;

            // Only queue-waiting single-player rooms are removed on leave.
            // Active battles must be finalized exclusively by CloseRound battle logic.
            if (room.Players.Count == 1 && _waitingBattleId == battleId)
            {
                _rooms.Remove(battleId);
                _waitingBattleId = null;
                return true;
            }

            // Non-destructive ack for active battle rooms.
            return true;
        }
    }
}

public class JoinResponse
{
    public string BattleId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string Status { get; set; } = ""; // "waiting" | "battle"
    public BattleStartedPayloadDto? BattleStarted { get; set; }
}
