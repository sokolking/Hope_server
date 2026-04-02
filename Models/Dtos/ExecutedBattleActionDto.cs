using System.Text.Json.Serialization;


namespace BattleServer.Models;

public class ExecutedBattleActionDto
{
    public string ActionType { get; set; } = "";
    public int Tick { get; set; }
    /// <summary><see cref="BattleExecutedActionStatuses"/>.</summary>
    public string ActionStatus { get; set; } = BattleExecutedActionStatuses.Failure;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FailureReason { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FromHex { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToHex { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TargetUnitId { get; set; }
    /// <summary>FK to <c>body_parts.id</c>; 0 = not specified.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int BodyPart { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Posture { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Damage { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Healed { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool TargetDied { get; set; }
    /// <summary>Итоговая вероятность попадания (0…1) после дистанции, укрытия и меткости; null если броска не было (стена, промах валидации и т.п.).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? HitProbability { get; set; }
    /// <summary>Результат броска по <see cref="HitProbability"/>; null если броска не было.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HitSucceeded { get; set; }
}
