using System.Text.Json;
using helengine.editor;

namespace helengine.switch_platform.builder;

/// <summary>Reads and writes the editor's texture cook JSON, validating exact Switch-owned format and alpha choices.</summary>
public static class SwitchTextureCookSettingsSerializer {
    /// <summary>Serializes validated native or legacy generic settings without changing stable platform IDs.</summary>
    public static string Serialize(TextureAssetProcessorSettings settings) {
        Validate(settings);
        return JsonSerializer.Serialize(new { maxResolution = settings.MaxResolution, colorFormat = settings.ColorFormatId, alphaPrecision = settings.AlphaPrecision.ToString(), indexingMethod = settings.IndexingMethodId });
    }
    /// <summary>Reads required fields and rejects unknown enum values, missing settings and unsupported format combinations.</summary>
    public static TextureAssetProcessorSettings Deserialize(string payload) {
        if (string.IsNullOrWhiteSpace(payload)) throw new ArgumentException("Switch texture settings are required.");
        using JsonDocument document = JsonDocument.Parse(payload); JsonElement root = document.RootElement;
        if (!root.TryGetProperty("maxResolution", out JsonElement maximum) || !maximum.TryGetInt32(out int cap)
            || !root.TryGetProperty("colorFormat", out JsonElement color) || color.ValueKind != JsonValueKind.String
            || !root.TryGetProperty("alphaPrecision", out JsonElement coverage) || coverage.ValueKind != JsonValueKind.String
            || !Enum.TryParse(coverage.GetString(), false, out TextureAssetAlphaPrecision alpha) || !Enum.IsDefined(alpha)) throw new ArgumentException("Malformed Switch texture cook settings.");
        TextureAssetProcessorSettings settings = new TextureAssetProcessorSettings { MaxResolution = cap, ColorFormatId = color.GetString(), AlphaPrecision = alpha };
        if (root.TryGetProperty("indexingMethod", out JsonElement indexing)) settings.IndexingMethodId = indexing.GetString();
        Validate(settings); return settings;
    }
    /// <summary>Checks only formats actually published for Switch image/font cooking.</summary>
    static void Validate(TextureAssetProcessorSettings settings) {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        if (settings.ColorFormatId != null && settings.ColorFormatId.StartsWith("Switch.", StringComparison.Ordinal)) { SwitchNativeTextureCooker.ValidateSettings(settings); return; }
        if (settings.MaxResolution < 0 || settings.MaxResolution > 16384 || (settings.ColorFormatId != "Rgba32" && settings.ColorFormatId != "Rgba4444" && settings.ColorFormatId != "Indexed4" && settings.ColorFormatId != "Indexed8")
            || (settings.AlphaPrecision != TextureAssetAlphaPrecision.Opaque && settings.AlphaPrecision != TextureAssetAlphaPrecision.Binary && settings.AlphaPrecision != TextureAssetAlphaPrecision.A4 && settings.AlphaPrecision != TextureAssetAlphaPrecision.A8)) throw new ArgumentException("Unsupported Switch generic texture settings.");
    }
}
