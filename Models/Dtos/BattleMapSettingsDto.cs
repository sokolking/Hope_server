namespace BattleServer.Models;

/// <summary>Размер поля боя (ширина = колонки, высота = строки). Hex-тег: буква = col, число = row.</summary>
public sealed class BattleMapSettingsDto
{
    public int MapWidth { get; set; } = 25;
    public int MapHeight { get; set; } = 40;
}
