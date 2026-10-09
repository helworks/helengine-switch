using helengine.baseplatform.Definitions;

namespace helengine.switch_platform.builder;

/// <summary>Publishes exact native Switch format/alpha combinations with the existing generic image and font settings.</summary>
public static class SwitchTextureFormatCapabilities {
    /// <summary>Creates all 258 native layout selections and generic compatibility capabilities.</summary>
    public static PlatformTextureFormatCapabilityDefinition Create() {
        List<string> formats = new List<string> { "Rgba32", "Rgba4444", "Indexed4", "Indexed8" };
        List<PlatformTextureFormatCombinationDefinition> combinations = new List<PlatformTextureFormatCombinationDefinition>();
        foreach (string id in formats) foreach (TextureAssetAlphaPrecision alpha in new[] { TextureAssetAlphaPrecision.Opaque, TextureAssetAlphaPrecision.Binary, TextureAssetAlphaPrecision.A4, TextureAssetAlphaPrecision.A8 }) combinations.Add(new PlatformTextureFormatCombinationDefinition(id, alpha));
        foreach (SwitchTextureFormat format in SwitchTextureFormatCatalog.Formats) {
            formats.Add(format.Id); combinations.Add(new PlatformTextureFormatCombinationDefinition(format.Id, TextureAssetAlphaPrecision.Opaque));
            if (format.Alpha != TextureAssetAlphaPrecision.Opaque) combinations.Add(new PlatformTextureFormatCombinationDefinition(format.Id, format.Alpha));
        }
        return new PlatformTextureFormatCapabilityDefinition(formats.ToArray(), new[] { TextureAssetAlphaPrecision.Opaque, TextureAssetAlphaPrecision.Binary, TextureAssetAlphaPrecision.A2, TextureAssetAlphaPrecision.A4, TextureAssetAlphaPrecision.A8 }, combinations.ToArray());
    }
}
