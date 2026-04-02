using System;


namespace BattleServer.Models;

/// <summary>Одна ячейка инвентаря пользователя (0..11).</summary>
public class UserInventorySlotDto
{
    public int SlotIndex { get; set; }
    public long? ItemId { get; set; }
    public string? ItemName { get; set; }
    public string ItemType { get; set; } = "weapon";
    public int DamageMin { get; set; }
    public int DamageMax { get; set; }
    public int Range { get; set; }
    public string IconKey { get; set; } = "fist";
    public int UseApCost { get; set; }
    public int ReloadApCost { get; set; }
    public long? AmmoTypeId { get; set; }
    public int MagazineSize { get; set; }
    /// <summary>Primary cell of a multi-slot item: width (1 or 2). Continuation cells use 0.</summary>
    public int SlotSpan { get; set; }
    /// <summary>True when this stack is currently equipped (primary cell only).</summary>
    public bool Equipped { get; set; }
    /// <summary>Second cell of a 2-slot weapon.</summary>
    public bool Continuation { get; set; }
    /// <summary>True for countable stack items (for example ammo).</summary>
    public bool Stackable { get; set; }
    /// <summary>Stack amount for <see cref="Stackable"/> items.</summary>
    public int Quantity { get; set; }
    /// <summary>
    /// Count from <c>user_inventory_items.rounds</c> (uses left / stack size); ammo stacks: same as stack count in rounds.
    /// Clients should prefer this over <see cref="Quantity"/> for display.
    /// </summary>
    public int Rounds { get; set; }
    /// <summary>Rounds currently loaded in weapon chamber/magazine for this inventory item.</summary>
    public int ChamberRounds { get; set; }
    /// <summary>Whether this item can be equipped in hand.</summary>
    public bool IsEquippable { get; set; }
}
