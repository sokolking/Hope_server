using System;


namespace BattleServer.Models;

/// <summary>Параметры сужения игрового поля по раундам (таблица battle_zone_shrink).</summary>
public class BattleZoneShrinkRowDto
{
    /// <summary>Первый раунд (нумерация с 1), с которого действует сужение.</summary>
    public int ShrinkStartRound { get; set; } = 10;
    /// <summary>Каждые сколько раундов сужать по горизонтали (слева и справа).</summary>
    public int HorizontalShrinkInterval { get; set; } = 2;
    /// <summary>Сколько колонок убирать с каждой стороны за шаг по горизонтали.</summary>
    public int HorizontalShrinkAmount { get; set; } = 2;
    /// <summary>Каждые сколько раундов сужать по вертикали (сверху и снизу).</summary>
    public int VerticalShrinkInterval { get; set; } = 2;
    /// <summary>Сколько рядов убирать с каждой стороны за шаг по вертикали.</summary>
    public int VerticalShrinkAmount { get; set; } = 1;
    /// <summary>Минимальная ширина активной зоны (число колонок).</summary>
    public int MinWidth { get; set; } = 5;
    /// <summary>Минимальная высота активной зоны (число рядов).</summary>
    public int MinHeight { get; set; } = 3;

    public static BattleZoneShrinkRowDto Defaults => new BattleZoneShrinkRowDto();
}
