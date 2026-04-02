using System;


namespace BattleServer.Models;

/// <summary>Обновление пользователя из админки /users (игрок сам характеристики не меняет). Пароль: null — не менять.</summary>
public class UserUpdateRequest
{
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string? Password { get; set; }
    public int Experience { get; set; }
    public int Strength { get; set; }
    public int Endurance { get; set; }
    public int Accuracy { get; set; }
    public int MaxHp { get; set; }
    public int MaxAp { get; set; }
}
