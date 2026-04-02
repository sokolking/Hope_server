using BattleServer.Models;
using Npgsql;

namespace BattleServer;

/// <summary>Глобальные размеры поля боя (singleton id=1). Новые комнаты читают значение при создании.</summary>
public sealed class BattleMapSettingsDatabase
{
    public const int MaxMapWidth = 26;
    public const int MaxMapHeight = 99;

    private readonly BattlePostgresDatabase _database;

    public BattleMapSettingsDatabase(BattlePostgresDatabase database)
    {
        _database = database;
    }

    /// <summary>Создаёт таблицу и строку по умолчанию при отсутствии (для существующих БД без полного bootstrap).</summary>
    public void EnsureSchema()
    {
        using var connection = _database.DataSource.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
CREATE TABLE IF NOT EXISTS battle_map_settings (
    id INT PRIMARY KEY,
    map_width INT NOT NULL DEFAULT 25,
    map_height INT NOT NULL DEFAULT 40,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
INSERT INTO battle_map_settings (id, map_width, map_height) VALUES (1, 25, 40)
ON CONFLICT (id) DO NOTHING;
""";
        command.ExecuteNonQuery();
    }

    public (int Width, int Height) GetMapDimensions()
    {
        try
        {
            using var connection = _database.DataSource.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
SELECT map_width, map_height FROM battle_map_settings WHERE id = 1 LIMIT 1;
""";
            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return (HexSpawn.DefaultGridWidth, HexSpawn.DefaultGridLength);
            int w = Math.Clamp(reader.GetInt32(0), 1, MaxMapWidth);
            int h = Math.Clamp(reader.GetInt32(1), 1, MaxMapHeight);
            return (w, h);
        }
        catch
        {
            return (HexSpawn.DefaultGridWidth, HexSpawn.DefaultGridLength);
        }
    }

    public BattleMapSettingsDto GetSettings()
    {
        var (w, h) = GetMapDimensions();
        return new BattleMapSettingsDto { MapWidth = w, MapHeight = h };
    }

    public void UpsertSettings(BattleMapSettingsDto row)
    {
        int w = Math.Clamp(row.MapWidth, 1, MaxMapWidth);
        int h = Math.Clamp(row.MapHeight, 1, MaxMapHeight);
        using var connection = _database.DataSource.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
INSERT INTO battle_map_settings (id, map_width, map_height, updated_at)
VALUES (1, @w, @h, now())
ON CONFLICT (id) DO UPDATE SET
    map_width = EXCLUDED.map_width,
    map_height = EXCLUDED.map_height,
    updated_at = EXCLUDED.updated_at;
""";
        command.Parameters.AddWithValue("w", w);
        command.Parameters.AddWithValue("h", h);
        command.ExecuteNonQuery();
    }
}
