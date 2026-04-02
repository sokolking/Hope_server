using System;


namespace BattleServer.Models;

public sealed class UserItemReplaceDto
{
    public long? ItemId { get; set; }
    public long? AmmoTypeId { get; set; }
    public string ItemType { get; set; } = "";
    public int Quantity { get; set; }
    public int ChamberRounds { get; set; }
    public int StartSlot { get; set; } = -1;
    public bool IsEquipped { get; set; }
    public bool IsEquippable { get; set; }
}
