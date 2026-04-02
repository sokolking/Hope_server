using System;


namespace BattleServer.Models;

/// <summary>Полная запись для upsert в <c>weapons</c>.</summary>
public sealed class BattleWeaponUpsertDto
{
    public long ItemId { get; set; }
    public string Name { get; set; } = "";
    public int DamageMin { get; set; } = 1;
    public int DamageMax { get; set; } = 1;
    public int Range { get; set; }
    public string IconKey { get; set; } = "";
    public int AttackApCost { get; set; } = 1;
    /// <summary>Кучность 0…1 (выше — лучше). В БД колонка <c>spread_penalty</c>.</summary>
    public double Tightness { get; set; } = 1.0;
    public int TrajectoryHeight { get; set; } = 1;
    public int Quality { get; set; } = 100;
    public int WeaponCondition { get; set; } = 100;
    public bool IsSniper { get; set; }
    public double Mass { get; set; }
    public long? AmmoTypeId { get; set; }
    public int ArmorPierce { get; set; }
    public int MagazineSize { get; set; }
    public int ReloadApCost { get; set; }
    public string Category { get; set; } = "cold";
    public int ReqLevel { get; set; } = 1;
    public int ReqStrength { get; set; }
    public int ReqEndurance { get; set; }
    public int ReqAccuracy { get; set; }
    public string ReqMasteryCategory { get; set; } = "";
    public int StatEffectStrength { get; set; }
    public int StatEffectEndurance { get; set; }
    public int StatEffectAccuracy { get; set; }
    public string DamageType { get; set; } = "physical";
    public int BurstRounds { get; set; }
    public int BurstApCost { get; set; }
    /// <summary>1 or 2 inventory cells per instance.</summary>
    public int InventorySlotWidth { get; set; } = 1;
    /// <summary>Hand slots consumed by item in inventory grid: 0, 1 or 2.</summary>
    public int InventoryGrid { get; set; } = 1;
    public bool IsEquippable { get; set; } = true;
    /// <summary>Item type written to <c>items.type</c>: weapon/ammo/medicine.</summary>
    public string ItemType { get; set; } = "weapon";
}
