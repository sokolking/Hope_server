using System;
using System.Text.Json.Serialization;


namespace BattleServer.Models;

/// <summary>Совместимо с Unity (сериализация в camelCase).</summary>
public class HexPositionDto
{
    [JsonIgnore]
    public int Col { get; set; }
    [JsonIgnore]
    public int Row { get; set; }
    public string Hex
    {
        get => ToHex(Col, Row);
        set
        {
            if (TryParseHex(value, out int col, out int row))
            {
                Col = col;
                Row = row;
            }
        }
    }

    private static string ToHex(int col, int row)
    {
        int safeCol = Math.Clamp(col, 0, 25);
        int safeRow = Math.Clamp(row, 0, 99);
        return $"{(char)('A' + safeCol)}{safeRow}";
    }

    private static bool TryParseHex(string? hex, out int col, out int row)
    {
        col = 0;
        row = 0;
        if (string.IsNullOrWhiteSpace(hex))
            return false;
        string s = hex.Trim().ToUpperInvariant();
        if (s.Length < 2 || s.Length > 3)
            return false;
        char letter = s[0];
        if (letter < 'A' || letter > 'Z')
            return false;
        if (!int.TryParse(s.Substring(1), out row))
            return false;
        if (row < 0 || row > 99)
            return false;
        col = letter - 'A';
        return true;
    }
}
