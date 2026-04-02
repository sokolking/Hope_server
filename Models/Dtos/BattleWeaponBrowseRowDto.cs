using System;


namespace BattleServer.Models;

/// <summary>Строка из таблицы weapons (список/поиск по коду).</summary>
public class BattleWeaponBrowseRowDto
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Диапазон урона в бою (случайное целое inclusive).</summary>
    public int DamageMin { get; set; } = 1;
    public int DamageMax { get; set; } = 1;
    /// <summary>Weapon range in hexes. In DB/API list, <c>-1</c> means not applicable; combat resolves to adjacent melee (1).</summary>
    public int Range { get; set; }
    public string IconKey { get; set; } = "";
    /// <summary>Single attack AP cost. In DB, <c>-1</c> means N/A; combat uses 1.</summary>
    public int AttackApCost { get; set; } = 1;
    /// <summary>Кучность (0…1): чем выше — тем кучнее, тем выше шанс попадания. Колонка БД <c>spread_penalty</c> (историческое имя).</summary>
    public double Tightness { get; set; } = 1.0;
    /// <summary>Trajectory height for LoS (0…3). In DB, <c>-1</c> means N/A; combat uses 0.</summary>
    public int TrajectoryHeight { get; set; } = 1;
    /// <summary>Только БД; в бой пока не входит.</summary>
    public int Quality { get; set; } = 100;
    /// <summary>Только БД; в бой пока не входит. Колонка <c>weapon_condition</c>.</summary>
    public int WeaponCondition { get; set; } = 100;
    /// <summary>Ослабленный штраф к p за дистанцию за пределами <see cref="Range"/> (кривая «снайпер»).</summary>
    public bool IsSniper { get; set; }
    public double Mass { get; set; }
    public long? AmmoTypeId { get; set; }
    public string AmmoName { get; set; } = "";
    /// <summary>In DB, <c>-1</c> means N/A; combat uses 0.</summary>
    public int ArmorPierce { get; set; }
    /// <summary>In DB, <c>-1</c> means N/A; combat uses 0.</summary>
    public int MagazineSize { get; set; }
    /// <summary>Reload AP cost. In DB, <c>-1</c> means N/A; combat uses 0.</summary>
    public int ReloadApCost { get; set; }
    public string Category { get; set; } = "cold";
    /// <summary>Min character level. In DB, <c>-1</c> means N/A; combat uses 0 (no level gate).</summary>
    public int ReqLevel { get; set; } = 1;
    /// <summary>In DB, <c>-1</c> means N/A; combat uses 0.</summary>
    public int ReqStrength { get; set; }
    /// <summary>In DB, <c>-1</c> means N/A; combat uses 0.</summary>
    public int ReqEndurance { get; set; }
    /// <summary>In DB, <c>-1</c> means N/A; combat uses 0.</summary>
    public int ReqAccuracy { get; set; }
    /// <summary>Владение по категории (ключ навыка).</summary>
    public string ReqMasteryCategory { get; set; } = "";
    /// <summary>Stat deltas. Exactly <c>-1</c> in DB means N/A (combat uses 0); other values (including negatives) are kept.</summary>
    public int StatEffectStrength { get; set; }
    public int StatEffectEndurance { get; set; }
    public int StatEffectAccuracy { get; set; }
    public string DamageType { get; set; } = "physical";
    /// <summary>Burst rounds per use. In DB, <c>-1</c> means N/A; combat uses 0.</summary>
    public int BurstRounds { get; set; }
    /// <summary>Burst AP cost. In DB, <c>-1</c> means N/A; combat uses 0.</summary>
    public int BurstApCost { get; set; }
    /// <summary>Inventory grid width: 1 or 2 cells (server clamps).</summary>
    public int InventorySlotWidth { get; set; } = 1;
    /// <summary>Hand slots consumed by item in inventory grid: 0, 1 or 2.</summary>
    public int InventoryGrid { get; set; } = 1;
    /// <summary>Common item flag from <c>items.is_equippable</c>.</summary>
    public bool IsEquippable { get; set; }
    /// <summary>Common item type from <c>items.type</c> (weapon/ammo/medicine).</summary>
    public string ItemType { get; set; } = "weapon";
}
