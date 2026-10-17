using helengine;
using helengine.editor;
using helengine.baseplatform.Manifest;

namespace helengine.switch_platform.builder.tests;

/// <summary>Verifies that editor-externalized font atlases remain the authoritative texture cook input.</summary>
public sealed class SwitchExternalFontAtlasTests {
    /// <summary>Cooks serialized atlas sources without replacing them with the metric-only packaged font or changing its bytes.</summary>
    [Theory]
    [InlineData(".hetex")]
    [InlineData(".hasset")]
    public void Work_item_preserves_external_atlas_reference_and_coverage(string extension) {
        string root = Environment.GetEnvironmentVariable("HELENGINE_SWITCH_MENU_TEST_ROOT")
            ?? throw new InvalidOperationException("A fixed menu test root is required.");
        string fixture = Path.Combine(root, extension.TrimStart('.'));
        string sourcePath = Path.Combine(fixture, "source", "body" + extension);
        string package = Path.Combine(fixture, "package-source");
        string fontPath = Path.Combine(package, "cooked", "fonts", "body.hefont");
        string texturePath = Path.ChangeExtension(fontPath, ".hetex");
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath));
        Directory.CreateDirectory(Path.GetDirectoryName(fontPath));
        using TextureAsset source = SwitchNativeTextureTests.Pattern(256, 1);
        for (int index = 0; index < 256; index++) {
            source.Colors[index * 4] = 255;
            source.Colors[index * 4 + 1] = 255;
            source.Colors[index * 4 + 2] = 255;
            source.Colors[index * 4 + 3] = (byte)index;
        }
        byte[] sourceBytes = helengine.files.AssetSerializer.SerializeToBytes(source);
        File.WriteAllBytes(sourcePath, sourceBytes);
        using FontAsset font = new FontAsset(new FontInfo("Externalized body", 32, 8), null, new Dictionary<char, FontChar>(), 32, 256, 1) {
            CookedAtlasTextureRelativePath = "cooked/fonts/body.hetex"
        };
        using (FileStream stream = File.Create(fontPath)) helengine.files.FontAssetBinarySerializer.Serialize(stream, font);
        byte[] fontBytes = File.ReadAllBytes(fontPath);
        var settings = new TextureAssetProcessorSettings { ColorFormatId = "Rgba32", AlphaPrecision = TextureAssetAlphaPrecision.A8 };
        PlatformCookWorkItem item = new PlatformCookWorkItem("body-atlas", sourcePath, "font-atlas-texture", "switch", "runtime-texture",
            "cooked/fonts/body.hetex", "body-atlas", "source-hash", "settings-hash", SwitchTextureCookSettingsSerializer.Serialize(settings),
            [new PlatformCookWorkItemMetadata("source-asset-id", "body")]);
        new SwitchTextureWorkItemCooker().Cook(item, package);
        using FileStream cookedStream = File.OpenRead(texturePath);
        using TextureAsset cooked = Assert.IsType<TextureAsset>(PackagedAssetBinarySerializer.Deserialize(cookedStream));
        Assert.Equal(source.Colors, cooked.Colors);
        Assert.Equal("body#atlas", cooked.Id);
        Assert.Equal(RuntimeAssetIdGenerator.Generate("body#atlas"), cooked.RuntimeAssetId);
        Assert.Equal(sourceBytes, File.ReadAllBytes(sourcePath));
        Assert.Equal(fontBytes, File.ReadAllBytes(fontPath));
        using FileStream fontStream = File.OpenRead(fontPath);
        using FontAsset restored = helengine.files.FontAssetBinarySerializer.Deserialize(fontStream);
        Assert.Null(restored.SourceTextureAsset);
        Assert.Equal("cooked/fonts/body.hetex", restored.CookedAtlasTextureRelativePath);
        Assert.Equal(32, restored.LineHeight);
        Assert.Equal(256, restored.AtlasWidth);
    }
}
