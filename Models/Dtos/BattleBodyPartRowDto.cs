using System;


namespace BattleServer.Models;

/// <summary>One row from <c>body_parts</c> (id + stable English code).</summary>
public class BattleBodyPartRowDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
}
