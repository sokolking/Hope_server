using System;


namespace BattleServer.Models;

public sealed class UserItemAdminDto
{
    public long? ItemId { get; set; }
    public long? AmmoTypeId { get; set; }
    public string ItemType { get; set; } = "";
    public string Name { get; set; } = "";
    public string IconKey { get; set; } = "";
    public int Quality { get; set; } = 100;
    public int Condition { get; set; } = 100;
    public double Mass { get; set; }
    public int InventoryGrid { get; set; } = 1;
    public int Quantity { get; set; }
    public int ChamberRounds { get; set; }
    public int StartSlot { get; set; } = -1;
    public bool IsEquipped { get; set; }
    public bool IsEquippable { get; set; }
    public bool IsStackable { get; set; }
    public int SlotWidth { get; set; }
}
