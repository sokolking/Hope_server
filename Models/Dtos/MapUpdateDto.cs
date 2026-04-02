using System;


namespace BattleServer.Models;

public class MapUpdateDto
{
    public int Tick { get; set; }
    public string Hex { get; set; } = "";
    public CellObjectState NewState { get; set; }
}
