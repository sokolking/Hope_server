using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace BattleServer;

/// <summary>
/// WebSocket-подключения по battleId для пуша результата раунда (без SignalR на клиенте Unity).
/// </summary>
public static class BattleWebSocketRegistry
{
    private sealed class Entry
    {
        public required WebSocket Ws;
        /// <summary>Участник боя или <c>__spectator__</c>.</summary>
        public required string PlayerId;
    }

    private static readonly Dictionary<string, List<Entry>> Sockets = new();
    private static readonly object Gate = new();

    public static void Add(string battleId, WebSocket socket, string? connectionLabel, string playerId)
    {
        if (string.IsNullOrEmpty(battleId) || socket == null) return;
        int total;
        lock (Gate)
        {
            if (!Sockets.TryGetValue(battleId, out var list))
            {
                list = new List<Entry>();
                Sockets[battleId] = list;
            }

            list.Add(new Entry { Ws = socket, PlayerId = playerId ?? "" });
            total = list.Count;
        }

        var label = string.IsNullOrEmpty(connectionLabel) ? "?" : connectionLabel;
        Console.WriteLine($"[BattleWS] registry add: battleId={battleId}, conn={label}, playerId={playerId ?? ""}, openSocketsInBattle={total}, utc={DateTime.UtcNow:O}");
    }

    public static void Remove(string battleId, WebSocket socket, string? connectionLabel = null)
    {
        if (string.IsNullOrEmpty(battleId) || socket == null) return;
        int remaining;
        lock (Gate)
        {
            if (!Sockets.TryGetValue(battleId, out var list)) return;
            list.RemoveAll(e => ReferenceEquals(e.Ws, socket));
            remaining = list.Count;
            if (list.Count == 0)
                Sockets.Remove(battleId);
        }

        var label = string.IsNullOrEmpty(connectionLabel) ? "?" : connectionLabel;
        Console.WriteLine($"[BattleWS] registry remove: battleId={battleId}, conn={label}, remainingInBattle={remaining}, utc={DateTime.UtcNow:O}");
    }

    public static async Task BroadcastTextAsync(string battleId, string json, CancellationToken ct = default)
    {
        List<WebSocket>? copy;
        lock (Gate)
        {
            if (!Sockets.TryGetValue(battleId, out var list) || list.Count == 0)
            {
                Console.WriteLine($"[BattleWS] broadcast skip: battleId={battleId}, reason=no_subscribers, utc={DateTime.UtcNow:O}");
                return;
            }

            copy = list.Where(e => e.Ws.State == WebSocketState.Open).Select(e => e.Ws).ToList();
        }

        if (copy.Count == 0)
        {
            Console.WriteLine($"[BattleWS] broadcast skip: battleId={battleId}, reason=all_sockets_closed, utc={DateTime.UtcNow:O}");
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(json);
        var previewLen = Math.Min(180, json.Length);
        var preview = previewLen < json.Length ? json[..previewLen] + "…" : json;
        Console.WriteLine($"[BattleWS] broadcast: battleId={battleId}, recipients={copy.Count}, bytes={bytes.Length}, preview={preview}, utc={DateTime.UtcNow:O}");

        var idx = 0;
        foreach (var ws in copy)
        {
            idx++;
            try
            {
                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
                Console.WriteLine($"[BattleWS] send ok: battleId={battleId}, socket#{idx}, state={ws.State}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BattleWS] send fail: battleId={battleId}, socket#{idx}, {ex.Message}");
            }
        }
    }

    /// <summary>Отправить JSON только сокетам союзников (та же команда PvP), не отправителю и не зрителям. Используется для planningArrow и planningMark.</summary>
    public static async Task SendPlanningOverlayToAlliesAsync(
        string battleId,
        WebSocket senderWs,
        string senderPlayerId,
        BattleRoom room,
        string json,
        string logLabel,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(battleId) || room == null || !room.IsPvpTeamBattle)
            return;

        int senderTeam = room.ComputePvpTeamIdForPlayer(senderPlayerId);
        List<WebSocket>? targets;
        lock (Gate)
        {
            if (!Sockets.TryGetValue(battleId, out var list) || list.Count == 0)
                return;
            targets = new List<WebSocket>();
            foreach (var e in list)
            {
                if (e.Ws.State != WebSocketState.Open)
                    continue;
                if (ReferenceEquals(e.Ws, senderWs))
                    continue;
                if (string.IsNullOrEmpty(e.PlayerId) || string.Equals(e.PlayerId, "__spectator__", StringComparison.Ordinal))
                    continue;
                if (room.ComputePvpTeamIdForPlayer(e.PlayerId) != senderTeam)
                    continue;
                targets.Add(e.Ws);
            }
        }

        if (targets == null || targets.Count == 0)
            return;

        var bytes = Encoding.UTF8.GetBytes(json);
        for (int i = 0; i < targets.Count; i++)
        {
            try
            {
                await targets[i].SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BattleWS] {logLabel} ally send fail: battleId={battleId}, i={i}, {ex.Message}");
            }
        }
    }
}
