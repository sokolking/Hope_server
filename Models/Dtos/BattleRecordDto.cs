using System;
using System.Collections.Generic;


namespace BattleServer.Models;

public class BattleRecordDto
{
    public string BattleId { get; set; } = "";
    public List<string> TurnIds { get; set; } = new();
}
