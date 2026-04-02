namespace BattleServer.Models;

/// <summary>Строковые значения <c>actionType</c> для очереди хода и журнала выполненных действий (wire JSON).</summary>
public static class BattleActionTypes
{
    public const string MoveStep = "MoveStep";
    public const string Attack = "Attack";
    public const string ChangePosture = "ChangePosture";
    public const string Wait = "Wait";
    public const string Reload = "Reload";
    public const string EquipWeapon = "EquipWeapon";
    public const string UseItem = "UseItem";
}
