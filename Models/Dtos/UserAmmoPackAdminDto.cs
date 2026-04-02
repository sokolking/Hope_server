using System;


namespace BattleServer.Models;

public sealed class UserAmmoPackAdminDto
{
    public long Id { get; set; }
    public long AmmoTypeId { get; set; }
    public long ItemId { get; set; }
    public string Caliber { get; set; } = "";
    public string Name { get; set; } = "";
    public double UnitWeight { get; set; }
    public int Quality { get; set; } = 100;
    public int Condition { get; set; } = 100;
    public string IconKey { get; set; } = "";
    public int InventoryGrid { get; set; } = 1;
    public string ItemType { get; set; } = "ammo";
    public int StartSlot { get; set; }
    public int RoundsCount { get; set; }
    public int PacksCount { get; set; }
    public int TotalRounds { get; set; }
}
