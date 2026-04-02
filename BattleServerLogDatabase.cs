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
}
