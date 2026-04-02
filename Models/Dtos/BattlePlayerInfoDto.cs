using System;
using System.Text.Json.Serialization;


namespace BattleServer.Models;

public class BattlePlayerInfoDto
{
    public string PlayerId { get; set; } = "";
    [JsonIgnore]
    public int Col { get; set; }
    [JsonIgnore]
    public int Row { get; set; }
    public string Hex => $"{(char)('A' + Math.Clamp(Col, 0, 25))}{Math.Clamp(Row, 0, 99)}";
}
