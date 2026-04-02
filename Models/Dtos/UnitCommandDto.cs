using System;


namespace BattleServer.Models;

/// <summary>Команда юнита на один раунд (расширяемо под разные типы действий).</summary>
public class UnitCommandDto
{
    public string UnitId { get; set; } = "";
    public string CommandType { get; set; } = "Move"; // пока только Move
    public QueuedBattleActionDto[]? Actions { get; set; }
}
