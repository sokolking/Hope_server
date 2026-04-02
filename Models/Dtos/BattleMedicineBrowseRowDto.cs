using System;


namespace BattleServer.Models;

/// <summary>Row from <c>medicine</c> joined with <c>items</c> (consumables — not weapons).</summary>
public sealed class BattleMedicineBrowseRowDto
{
    /// <summary><c>items.id</c> (same as equipped / inventory key).</summary>
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string IconKey { get; set; } = "";
    public double Mass { get; set; }
    public int Quality { get; set; } = 100;
    public int Condition { get; set; } = 100;
    public int AttackApCost { get; set; } = 1;
    public int ReqLevel { get; set; } = 1;
    public int ReqStrength { get; set; }
    public int ReqEndurance { get; set; }
    public int ReqAccuracy { get; set; }
    public string ReqMasteryCategory { get; set; } = "";
    public string EffectType { get; set; } = "";
    public string EffectSign { get; set; } = "positive";
    public int EffectMin { get; set; }
    public int EffectMax { get; set; }
    public string EffectTarget { get; set; } = "enemy";
    public int InventorySlotWidth { get; set; } = 1;
    public int InventoryGrid { get; set; } = 1;
    public bool IsEquippable { get; set; }
    public string ItemType { get; set; } = "medicine";
}
