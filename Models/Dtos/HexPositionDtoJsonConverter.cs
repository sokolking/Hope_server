using System.Text.Json;
using System.Text.Json.Serialization;

namespace BattleServer.Models;

/// <summary>
/// Десериализация: при наличии <c>col</c> и <c>row</c> они главные; иначе разбор legacy <c>hex</c>.
/// Сериализация: всегда <c>col</c> и <c>row</c>.
/// </summary>
public sealed class HexPositionDtoJsonConverter : JsonConverter<HexPositionDto?>
{
    public override HexPositionDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return null;

        bool hasCol = root.TryGetProperty("col", out JsonElement colEl);
        bool hasRow = root.TryGetProperty("row", out JsonElement rowEl);
        if (hasCol && hasRow)
        {
            return new HexPositionDto
            {
                Col = colEl.GetInt32(),
                Row = rowEl.GetInt32()
            };
        }

        if (root.TryGetProperty("hex", out JsonElement hexEl) && hexEl.ValueKind == JsonValueKind.String)
        {
            string? h = hexEl.GetString();
            if (!string.IsNullOrEmpty(h) && HexPositionDto.TryParseWireHex(h, out int c, out int r))
                return new HexPositionDto { Col = c, Row = r };
        }

        throw new JsonException("HexPositionDto requires both col and row, or a valid hex string.");
    }

    public override void Write(Utf8JsonWriter writer, HexPositionDto? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        writer.WriteNumber("col", value.Col);
        writer.WriteNumber("row", value.Row);
        writer.WriteEndObject();
    }
}
