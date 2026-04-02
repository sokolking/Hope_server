using System;


namespace BattleServer.Models;

public class ActiveZoneDto
{
    public string TopLeftHex { get; set; } = "A0";
    public string TopRightHex { get; set; } = "A0";
    public string BottomLeftHex { get; set; } = "A0";
    public string BottomRightHex { get; set; } = "A0";
}
