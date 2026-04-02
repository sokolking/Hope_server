using System;


namespace BattleServer.Models;

/// <summary>Состояние юнита для отдачи клиенту (срез).</summary>
public class UnitStateDto
{
    public string UnitId { get; set; } = "";
    public UnitType UnitType { get; set; }
    public int Col { get; set; }
    public int Row { get; set; }
    public int MaxAp { get; set; }
    public int CurrentAp { get; set; }
    public float PenaltyFraction { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public long WeaponItemId { get; set; }
    public int WeaponDamageMin { get; set; } = 1;
    public int WeaponDamage { get; set; } = 1;
    public int WeaponRange { get; set; } = 1;
    /// <summary>Стоимость атаки (ОД), фиксированная логикой боя.</summary>
    public int WeaponAttackApCost { get; set; } = 1;
    public int CurrentMagazineRounds { get; set; }
    /// <summary>Меткость: аддитивный бонус к p попадания (+2% за пункт после множителей дистанции и укрытия).</summary>
    public int Accuracy { get; set; } = 10;
    /// <summary>Кучность оружия <c>T</c> (0…1, выше — кучнее). В формуле попадания вычитается <c>clamp(1 − T, …)</c>. Колонка БД <c>spread_penalty</c> — историческое имя.</summary>
    public double WeaponTightness { get; set; } = 1.0;
    /// <summary>Высота траектории выстрела для ЛС и стен (0 низкая, 1 обычная, 2 высокая).</summary>
    public int WeaponTrajectoryHeight { get; set; } = 1;
    /// <summary>Снайперское оружие: иная кривая p по дистанции за пределами дальности (урон без изменений).</summary>
    public bool WeaponIsSniper { get; set; }
    /// <summary><see cref="BattlePostures"/>.</summary>
    public string Posture { get; set; } = BattlePostures.Walk;
    /// <summary>PvP team: 0 or 1 for human players; -1 for mobs / unset.</summary>
    public int TeamId { get; set; } = -1;
}
