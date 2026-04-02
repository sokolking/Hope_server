using BattleServer.Models;
using Npgsql;
using System.Text.Json;

namespace BattleServer;

public sealed class BattleServerLogDatabase
{
    private readonly BattlePostgresDatabase _db;

    public BattleServerLogDatabase(BattlePostgresDatabase db)
    {
        _db = db;
    }

    public void EnsureCreated()
    {
        using var connection = _db.DataSource.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
CREATE TABLE IF NOT EXISTS public.server_logs (
    id bigserial PRIMARY KEY,
    created_utc timestamptz NOT NULL DEFAULT now(),
    level text NOT NULL,
    source text NOT NULL,
    message text NOT NULL,
    payload_json jsonb NULL
);
CREATE INDEX IF NOT EXISTS ix_server_logs_created_utc ON public.server_logs(created_utc);
""";
        cmd.ExecuteNonQuery();
    }

    public void Save(BattleLogEntry entry)
    {
        using var connection = _db.DataSource.OpenConnection();
        using var tx = connection.BeginTransaction();

        // Requested policy: before each insert remove one row older than 7 days, if any.
        using (var deleteCmd = connection.CreateCommand())
        {
            deleteCmd.Transaction = tx;
            deleteCmd.CommandText = """
DELETE FROM public.server_logs
WHERE id = (
    SELECT id
    FROM public.server_logs
    WHERE created_utc < (now() - interval '7 days')
    ORDER BY created_utc ASC
    LIMIT 1
);
""";
            deleteCmd.ExecuteNonQuery();
        }

        using (var insertCmd = connection.CreateCommand())
        {
            insertCmd.Transaction = tx;
            insertCmd.CommandText = """
INSERT INTO public.server_logs(created_utc, level, source, message, payload_json)
VALUES (@created, @level, @source, @message, @payload::jsonb);
""";
            insertCmd.Parameters.AddWithValue("created", entry.Utc.UtcDateTime);
            insertCmd.Parameters.AddWithValue("level", entry.Level);
            insertCmd.Parameters.AddWithValue("source", entry.Tag);
            insertCmd.Parameters.AddWithValue("message", entry.Message);
            insertCmd.Parameters.AddWithValue("payload", JsonSerializer.Serialize(new
            {
                id = entry.Id,
                raw = entry.Raw
            }));
            insertCmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public IReadOnlyList<BattleLogEntry> GetRecent(int take)
    {
        int safeTake = Math.Clamp(take, 1, 2000);
        var rows = new List<BattleLogEntry>(safeTake);
        using var connection = _db.DataSource.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
SELECT id, created_utc, level, source, message, payload_json
FROM public.server_logs
ORDER BY id DESC
LIMIT @take;
""";
        cmd.Parameters.AddWithValue("take", safeTake);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            long id = reader.IsDBNull(0) ? 0 : reader.GetInt64(0);
            DateTimeOffset utc = reader.IsDBNull(1)
                ? DateTimeOffset.UtcNow
                : new DateTimeOffset(reader.GetDateTime(1), TimeSpan.Zero);
            string level = reader.IsDBNull(2) ? "info" : reader.GetString(2);
            string source = reader.IsDBNull(3) ? "system" : reader.GetString(3);
            string message = reader.IsDBNull(4) ? "" : reader.GetString(4);
            string raw = message;

            if (!reader.IsDBNull(5))
            {
                try
                {
                    using var doc = JsonDocument.Parse(reader.GetFieldValue<string>(5));
                    if (doc.RootElement.TryGetProperty("id", out var idProp) &&
                        idProp.ValueKind == JsonValueKind.Number &&
                        idProp.TryGetInt64(out var parsedId) &&
                        parsedId > 0)
                    {
                        id = parsedId;
                    }

                    if (doc.RootElement.TryGetProperty("raw", out var rawProp) &&
                        rawProp.ValueKind == JsonValueKind.String)
                    {
                        raw = rawProp.GetString() ?? message;
                    }
                }
                catch
                {
                    // Keep robust reading even if payload_json is malformed.
                }
            }

            rows.Add(new BattleLogEntry
            {
                Id = id,
                Utc = utc,
                Level = string.IsNullOrWhiteSpace(level) ? "info" : level,
                Tag = string.IsNullOrWhiteSpace(source) ? "system" : source,
                Message = message,
                Raw = raw
            });
        }

        rows.Reverse();
        return rows;
    }
}
