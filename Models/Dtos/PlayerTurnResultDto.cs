using System;


namespace BattleServer.Models;

public class PlayerTurnResultDto
{
    public string UnitId { get; set; } = "";
    public UnitType UnitType { get; set; } = UnitType.Player;
    public bool Accepted { get; set; }
    public int CurrentAp { get; set; }
    public float PenaltyFraction { get; set; }
    public string? RejectedReason { get; set; }
    public int CurrentHp { get; set; }
    /// <summary><see cref="UnitStatuses"/>.</summary>
    public string UnitStatus { get; set; } = UnitStatuses.Alive;
    public int? Level { get; set; }
    public ExecutedBattleActionDto[]? ExecutedActions { get; set; }
    public int TeamId { get; set; } = -1;
}
