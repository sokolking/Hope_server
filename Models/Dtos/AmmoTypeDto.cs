using System;


namespace BattleServer.Models;

public sealed class AmmoTypeDto
{
    public long Id { get; set; }
    public long ItemId { get; set; }
    public string Caliber { get; set; } = "";
    public string Name { get; set; } = "";
    public double UnitWeight { get; set; }
    public int Quality { get; set; } = 100;
    public int Condition { get; set; } = 100;
    public string IconKey { get; set; } = "";
    public int InventoryGrid { get; set; } = 1;
    /// <summary>Common item type from <c>items.type</c> (weapon/ammo/medicine).</summary>
    public string ItemType { get; set; } = "ammo";
    public string Category { get; set; } = "";
}
