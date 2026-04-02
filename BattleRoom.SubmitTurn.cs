using System.Collections.Generic;
using System.Linq;
using BattleServer.Models;

namespace BattleServer;

public partial class BattleRoom
{
    /// <summary>Принять ход. Возвращает true, если все участники сдали ход и раунд нужно закрыть.</summary>
    public bool SubmitTurn(SubmitTurnPayloadDto payload)
    {
        if (payload.RoundIndex != RoundIndex)
        {
            Console.WriteLine(
                $"[BattleRoom] SubmitTurn rejected: reason=round_mismatch battleId={BattleId} playerId={payload.PlayerId} clientRound={payload.RoundIndex} serverRound={RoundIndex}");
            return false;
        }

        if (!Players.ContainsKey(payload.PlayerId))
        {
            Console.WriteLine(
                $"[BattleRoom] SubmitTurn rejected: reason=unknown_player battleId={BattleId} playerId={payload.PlayerId}");
            return false;
        }

        if (Submissions.ContainsKey(payload.PlayerId))
        {
            Console.WriteLine(
                $"[BattleRoom] SubmitTurn rejected: reason=duplicate_submit battleId={BattleId} playerId={payload.PlayerId} round={RoundIndex} submissions={Submissions.Count}/{Players.Count}");
            return false;
        }

        // Сформировать команду юнита для новой серверной модели.
        if (!PlayerToUnitId.TryGetValue(payload.PlayerId, out var unitId) || string.IsNullOrEmpty(unitId))
        {
            // Same rule as EnsureUnitsInitialized: users.id as string, or negative guest slot.
            unitId = GetPlayerUnitId(payload.PlayerId);
            PlayerToUnitId[payload.PlayerId] = unitId;
            if (!Units.ContainsKey(unitId) && Players.TryGetValue(payload.PlayerId, out var pos))
            {
                Units[unitId] = new UnitStateDto
                {
                    UnitId = unitId,
                    UnitType = UnitType.Player,
                    TeamId = ComputePvpTeamIdForPlayer(payload.PlayerId),
                    Col = pos.col,
                    Row = pos.row,
                    MaxAp = DefaultPlayerMaxAp,
                    CurrentAp = GetUnitRoundStartAp(UnitType.Player, 0f),
                    PenaltyFraction = 0f,
                    MaxHp = DefaultPlayerMaxHp,
                    CurrentHp = DefaultPlayerMaxHp,
                    WeaponItemId = GetWeaponItemIdFromLegacyKey(DefaultUnarmedKey),
                    WeaponDamageMin = DefaultWeaponDamage,
                    WeaponDamage = DefaultWeaponDamage,
                    WeaponRange = DefaultWeaponRange,
                    WeaponAttackApCost = GetWeaponAttackApCostFromLegacyKey(DefaultUnarmedKey),
                    CurrentMagazineRounds = GetWeaponMagazineSizeFromLegacyKey(DefaultUnarmedKey),
                    Accuracy = 10,
                    WeaponTightness = 1.0,
                    WeaponTrajectoryHeight = 1,
                    WeaponIsSniper = false,
                    Posture = BattlePostures.Walk
                };
            }
        }

        // Magazine state is authoritative on server and must be consumed by executed actions.
        // Do not overwrite with client submit snapshot, otherwise queued shots can all fail
        // with "Magazine is empty" when client planned attacks down to zero.

        UnitCommands[unitId] = new UnitCommandDto
        {
            UnitId = unitId,
            CommandType = "Queue",
            Actions = payload.Actions ?? Array.Empty<QueuedBattleActionDto>()
        };

        Submissions[payload.PlayerId] = payload;
        SubmissionOrder.Add(payload.PlayerId);
        RefreshRoundTimeLeft();
        if (RoundTimeLeft > 0.01f)
            EndedTurnEarlyThisRound[payload.PlayerId] = true;

        // Pre-fill mob queues for logging / any reader; actual mob resolution in CloseRound uses
        // BuildMobActionForCurrentState (tick sim), not these queues. Never block round close on mobs —
        // they are not separate "submitters" and have no round timer slot.
        EnsureMobCommandsForCurrentRound();

        // Все игроки прислали ходы?
        bool allPlayersSubmitted = Submissions.Count >= Players.Count;
        Console.WriteLine(
            $"[BattleRoom] SubmitTurn accepted: battleId={BattleId} playerId={payload.PlayerId} round={RoundIndex} actionCount={(payload.Actions?.Length ?? 0)} submissionsNow={Submissions.Count}/{Players.Count} allSubmitted={allPlayersSubmitted}");

        return allPlayersSubmitted;
    }

    /// <summary>Статусы участников для опроса: кто сдал ход, кто досрочно.</summary>
    public BattleParticipantStatusDto[] BuildParticipantStatuses()
    {
        return ParticipantIds
            .Where(Players.ContainsKey)
            .Select(pid =>
            {
                bool isMob = PlayerToUnitId.TryGetValue(pid, out var uid) &&
                             Units.TryGetValue(uid, out var us) &&
                             us.UnitType == UnitType.Mob;

                return new BattleParticipantStatusDto
                {
                    PlayerId = pid,
                    HasSubmitted = isMob ? true : Submissions.ContainsKey(pid),
                    EndedTurnEarly = isMob ? true : EndedTurnEarlyThisRound.GetValueOrDefault(pid)
                };
            })
            .ToArray();
    }
}
