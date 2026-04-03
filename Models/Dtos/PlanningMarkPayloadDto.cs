namespace BattleServer.Models;

/// <summary>Клиент → сервер: метка-крест планирования на одной клетке для союзников. Только hex-метка (A0…Z99 или x{col}:{row}).</summary>
public sealed class PlanningMarkPayloadDto
{
    public string Type { get; set; } = "";
    public string BattleId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string Hex { get; set; } = "";
}
