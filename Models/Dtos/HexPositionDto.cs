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

    /// <summary>A0…Z99 или расширенный <c>x{col}:{row}</c> (как <see cref="FormatWireLabel"/>).</summary>
    public static bool TryParseWireLabel(string? value, out int col, out int row)
    {
        col = 0;
        row = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        string s = value.Trim();
        if (s.Length >= 3 && (s[0] == 'x' || s[0] == 'X'))
        {
            string sub = s.Substring(1);
            int colon = sub.IndexOf(':');
            if (colon <= 0 || colon >= sub.Length - 1)
                return false;
            if (!int.TryParse(sub.AsSpan(0, colon), out col))
                return false;
            if (!int.TryParse(sub.AsSpan(colon + 1), out row))
                return false;
            return true;
        }

        return TryParseWireHex(s, out col, out row);
    }

    /// <summary>Метка для JSON: компактно в A0…Z99, иначе <c>x{col}:{row}</c>.</summary>
    public static string FormatWireLabel(int col, int row)
    {
        if (col is >= 0 and <= 25 && row is >= 0 and <= 99)
            return $"{(char)('A' + col)}{row}";
        return $"x{col}:{row}";
    }
}
