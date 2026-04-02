using System;


namespace BattleServer.Models;

/// <summary>Row in <c>user_inventory_items</c> for admin GET/PUT.</summary>
public sealed class UserInventoryItemAdminDto
{
    public long Id { get; set; }
    public long ItemId { get; set; }
    public int StartSlot { get; set; }
    public int SlotWidth { get; set; } = 1;
    public int Rounds { get; set; }
    public int ChamberRounds { get; set; }
    public bool IsEquipped { get; set; }
}
