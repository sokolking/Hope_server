using System;


namespace BattleServer.Models;

public class BattleUserBrowseRowDto
{
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public int Experience { get; set; }
    public int Level { get; set; }
    public int Strength { get; set; }
    public int Endurance { get; set; }
    public int Accuracy { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int MaxAp { get; set; }
    public long? EquippedItemId { get; set; }
    public string EquippedItemDisplay { get; set; } = "";
}
