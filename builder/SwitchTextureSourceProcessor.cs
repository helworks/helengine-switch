using helengine.editor;

namespace helengine.switch_platform.builder;

/// <summary>Imports images or font atlases and transfers independently cooked textures to the Switch build graph.</summary>
public sealed class SwitchTextureSourceProcessor {
    /// <summary>Cooks a source image or font atlas with a stable runtime identity and releases imported buffers on success or failure.</summary>
    public TextureAsset Cook(string sourcePath, string assetId, TextureAssetProcessorSettings settings, bool fontAtlas) {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath)) throw new FileNotFoundException("Switch texture source is required.", sourcePath);
        if (string.IsNullOrWhiteSpace(assetId)) throw new ArgumentException("Switch texture identity is required.");
        TextureAsset source = Import(sourcePath, fontAtlas), cooked = null;
        try {
            cooked = settings.ColorFormatId.StartsWith("Switch.", StringComparison.Ordinal) ? SwitchNativeTextureCooker.Apply(source, settings) : new TextureAssetProcessor().Apply(source, settings);
            cooked.Id = assetId; cooked.RuntimeAssetId = RuntimeAssetIdGenerator.Generate(assetId); return cooked;
        } finally { if (!ReferenceEquals(source, cooked)) source.Dispose(); }
    }
    /// <summary>Imports BNTX or serialized assets portably and uses the established host image/font importers for raw source files.</summary>
    static TextureAsset Import(string path, bool fontAtlas) {
        using FileStream stream = File.OpenRead(path); string extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension == ".bntx") return new SwitchBntxTextureImporter().ImportTexture(stream);
        if (extension == ".hefont" || (fontAtlas && (extension == ".ttf" || extension == ".otf"))) {
            using FontAsset font = extension == ".hefont" ? helengine.files.FontAssetBinarySerializer.Deserialize(stream)
                : new GdiFontImporter(null).ImportFont(stream, new FontAssetProcessorSettings { PixelSize = FontAssetProcessorSettings.DefaultPixelSize });
            TextureAsset atlas = font.SourceTextureAsset ?? throw new InvalidDataException("Switch source font has no embedded atlas.");
            font.SourceTextureAsset = null; return atlas;
        }
        if (extension == ".hasset" || extension == ".hetex") {
            Asset asset = helengine.files.AssetSerializer.Deserialize(stream);
            if (asset is TextureAsset texture) return texture;
            if (asset is IDisposable disposable) disposable.Dispose();
            throw new InvalidDataException("Switch source asset is not a texture.");
        }
        if (extension == ".dds" || extension == ".tga" || extension == ".targa") return new PfimTextureImporter().ImportTexture(stream);
        return new GDITextureImporter().ImportTexture(stream);
    }
}
