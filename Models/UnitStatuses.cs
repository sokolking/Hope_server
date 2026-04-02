namespace BattleServer.Models;

/// <summary>Строковые значения <c>unitStatus</c> в <see cref="PlayerTurnResultDto"/> (wire JSON, lowercase).</summary>
public static class UnitStatuses
{
    public const string Alive = "alive";
    public const string Dead = "dead";
    public const string Escaping = "escaping";
    public const string Fled = "fled";
}
