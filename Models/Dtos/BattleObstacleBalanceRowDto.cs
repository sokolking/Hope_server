using System;


namespace BattleServer.Models;

/// <summary>Баланс препятствий (таблица battle_obstacle_balance).</summary>
public class BattleObstacleBalanceRowDto
{
    public int WallMaxHp { get; set; } = 35;
    /// <summary>Снижение шанса попадания (0–95), если цель за деревом.</summary>
    public int TreeCoverMissPercent { get; set; } = 15;
    /// <summary>Снижение шанса попадания (0–95), если цель за камнем и поза sit/hide.</summary>
    public int RockCoverMissPercent { get; set; } = 20;
    public int WallSegmentsCount { get; set; } = 10;
    public int RockCount { get; set; } = 5;
    public int TreeCount { get; set; } = 5;

    public static BattleObstacleBalanceRowDto Defaults => new BattleObstacleBalanceRowDto();
}
