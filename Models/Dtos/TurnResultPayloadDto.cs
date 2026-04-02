using System;


namespace BattleServer.Models;

public class TurnResultPayloadDto
{
    public string BattleId { get; set; } = "";
    public int RoundIndex { get; set; }
    public PlayerTurnResultDto[]? Results { get; set; }
    /// <summary>allSubmitted — все сдали ход до таймера; timerExpired — время раунда вышло.</summary>
    public string RoundResolveReason { get; set; } = "";
    public bool BattleFinished { get; set; }
    public MapUpdateDto[]? MapUpdates { get; set; }
    public ActiveZoneDto ActiveZone { get; set; } = new();
}
