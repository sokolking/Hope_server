using System;

namespace BattleServer.Models;

/// <summary>Строковые значения <c>actionStatus</c> в <see cref="ExecutedBattleActionDto"/> (wire JSON).</summary>
public static class BattleExecutedActionStatuses
{
    public const string Succeeded = "Succeeded";
    public const string Failure = "Failure";

    public static bool IsSucceeded(string? status) =>
        string.Equals(status, Succeeded, StringComparison.OrdinalIgnoreCase);
}
