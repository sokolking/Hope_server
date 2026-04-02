using System.Text.Json.Serialization;


namespace BattleServer.Models;

/// <summary>
/// Клетка в offset-гексах. В JSON предпочтительны <c>col</c> и <c>row</c> (в т.ч. вне 0…W−1 для побега);
/// компактный <c>hex</c> поддерживается только для диапазона A0…Z99.
/// </summary>
public class HexPositionDto
{
    [JsonPropertyName("col")]
    public int Col { get; set; }

    [JsonPropertyName("row")]
    public int Row { get; set; }

    /// <summary>Опционально в JSON; при десериализации используется, если нет col/row.</summary>
    [JsonPropertyName("hex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Hex { get; set; }

    /// <summary>Компактная строка для логов/UI (кламп в A0…Z99).</summary>
    public string ToWireHex()
    {
        int c = System.Math.Clamp(Col, 0, 25);
        int r = System.Math.Clamp(Row, 0, 99);
        return $"{(char)('A' + c)}{r}";
    }

    public static bool TryParseWireHex(string? hex, out int col, out int row)
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
