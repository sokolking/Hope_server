using System;


namespace BattleServer.Models;

/// <summary>Публичный срез прогресса. Характеристики меняются только через БД/админку, не игроком.</summary>
public class UserProgressProfileDto
{
    public string Username { get; set; } = "";
    public int Experience { get; set; }
    public int Level { get; set; }
    public int Strength { get; set; }
    public int Agility { get; set; }
    public int Intuition { get; set; }
    public int Endurance { get; set; }
    public int Accuracy { get; set; }
    public int Intellect { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int MaxAp { get; set; }
    public int HitBonusPercent { get; set; }
}
