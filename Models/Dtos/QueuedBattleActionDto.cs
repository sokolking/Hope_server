using System;


namespace BattleServer.Models;

public class QueuedBattleActionDto
{
    public string ActionType { get; set; } = "";
    public HexPositionDto? TargetPosition { get; set; }
    public string? TargetUnitId { get; set; }
    /// <summary>FK to <c>body_parts.id</c>; 0 = not specified.</summary>
    public int BodyPart { get; set; }
    public string? Posture { get; set; }
    public int Cost { get; set; } = 1;
    /// <summary>Для EquipWeapon: идентификатор предмета оружия (<c>items.id</c>).</summary>
    public long? WeaponItemId { get; set; }
    /// <summary>Для отмены EquipWeapon: стоимость атаки предыдущего оружия (клиент).</summary>
    public int PreviousWeaponAttackApCost { get; set; }
    /// <summary>Для EquipWeapon: стоимость атаки нового оружия (клиент).</summary>
    public int WeaponAttackApCost { get; set; }
    public int PreviousMagazineRounds { get; set; }
}
