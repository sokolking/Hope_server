namespace BattleServer.Models;

/// <summary>Клиент → сервер: нарисовать стрелку планирования для союзников. Только hex-метки (A0…Z99 или x{col}:{row}).</summary>
public sealed class PlanningArrowPayloadDto
{
    public string Type { get; set; } = "";
    public string BattleId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string FromHex { get; set; } = "";
    public string ToHex { get; set; } = "";
}
