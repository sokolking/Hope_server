using System;


namespace BattleServer.Models;

public class BattleStartedPayloadDto
{
    public string BattleId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public BattlePlayerInfoDto[]? Players { get; set; }
    public float RoundDuration { get; set; }
    public long RoundDeadlineUtcMs { get; set; }
    /// <summary>Current server round index (for resume after re-login).</summary>
    public int RoundIndex { get; set; }
    /// <summary>Ширина поля (колонки); клиент строит HexGrid до спавна.</summary>
    public int MapWidth { get; set; }
    /// <summary>Высота поля (строки).</summary>
    public int MapHeight { get; set; }
    /// <summary>Параллельно спавн-массивам: для игроков — боевой <c>unitId</c> (как в <c>TurnResult</c>, напр. decimal users.id или отрицательный гостевой слот); для мобов — <c>mob:…</c>. Не слот <c>P1</c>/<c>P2</c>.</summary>
    public string[]? SpawnPlayerIds { get; set; }
    public int[]? SpawnCols { get; set; }
    public int[]? SpawnRows { get; set; }
    public int[]? SpawnCurrentAps { get; set; }
    public int[]? SpawnMaxAps { get; set; }
    public int[]? SpawnMaxHps { get; set; }
    public int[]? SpawnCurrentHps { get; set; }
    public string[]? SpawnCurrentPostures { get; set; }
    public long[]? SpawnWeaponItemIds { get; set; }
    /// <summary>Parallel to <see cref="SpawnWeaponItemIds"/>; DB <c>weapons.category</c> (e.g. cold, light, medium).</summary>
    public string[]? SpawnWeaponCategories { get; set; }
    public int[]? SpawnWeaponDamages { get; set; }
    /// <summary>Параллельно <see cref="SpawnWeaponDamages"/> (макс.); мин. урон для отображения/логики клиента.</summary>
    public int[]? SpawnWeaponDamageMins { get; set; }
    public int[]? SpawnWeaponRanges { get; set; }
    public int[]? SpawnWeaponAttackApCosts { get; set; }
    public int[]? SpawnCurrentMagazineRounds { get; set; }
    public double[]? SpawnWeaponTightnesses { get; set; }
    public int[]? SpawnWeaponTrajectoryHeights { get; set; }
    public bool[]? SpawnWeaponIsSnipers { get; set; }
    public string[]? SpawnDisplayNames { get; set; }
    public int[]? SpawnLevels { get; set; }
    /// <summary>Parallel to <see cref="SpawnPlayerIds"/>; 0/1 for PvP players, -1 for mobs.</summary>
    public int[]? SpawnTeamIds { get; set; }
    /// <summary>Combat stats for unit inspect UI (parallel to <see cref="SpawnPlayerIds"/>).</summary>
    public int[]? SpawnStrengths { get; set; }
    public int[]? SpawnAgilities { get; set; }
    public int[]? SpawnIntuitions { get; set; }
    public int[]? SpawnEndurances { get; set; }
    public int[]? SpawnAccuracies { get; set; }
    public int[]? SpawnIntellects { get; set; }
    public int[]? ObstacleCols { get; set; }
    public int[]? ObstacleRows { get; set; }
    /// <summary>Теги: wall | tree | rock — параллельно obstacleCols/Rows.</summary>
    public string[]? ObstacleTags { get; set; }
    /// <summary>Yaw стен (градусы вокруг Y), параллельно obstacleCols/Rows.</summary>
    public float[]? ObstacleWallYaws { get; set; }
    public CellObject[]? MapState { get; set; }
    public ActiveZoneDto ActiveZone { get; set; } = new();
}
