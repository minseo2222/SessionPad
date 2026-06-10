using System.Text.Json;
using System.Text.Json.Serialization;
using SessionPad.App.Models;

namespace SessionPad.App.Services;

/// <summary>
/// Reads <see cref="NotePanelState"/> from JSON like the string enum converter, but a
/// value this build does not recognize (for example a panel state written by a newer
/// version) falls back to the default instead of throwing — so the rest of the note is
/// preserved rather than lost. Writing is unchanged (the enum name as a string).
/// </summary>
internal sealed class TolerantPanelStateConverter : JsonConverter<NotePanelState>
{
    public override NotePanelState Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();
            if (Enum.TryParse<NotePanelState>(text, ignoreCase: true, out var parsed))
            {
                return parsed;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number
            && reader.TryGetInt32(out var number)
            && Enum.IsDefined(typeof(NotePanelState), number))
        {
            return (NotePanelState)number;
        }

        return NotePanelState.CompactNote;
    }

    public override void Write(
        Utf8JsonWriter writer,
        NotePanelState value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
