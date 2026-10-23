using helengine;
using helengine.editor;

namespace helengine.switch_platform.builder.tests;

/// <summary>Exercises real source decoding, stable identities and independent texture ownership without a native SDK.</summary>
public sealed class SwitchTextureSourceProcessorTests {
    /// <summary>Original two-pixel PNG with opaque red and half-transparent blue, independent of a host image writer.</summary>
    const string RgbaPng = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAABCAYAAAD0In+KAAAADklEQVR4nGP4z8AAQg0AD3oDfnfpf5cAAAAASUVORK5CYII=";

    /// <summary>Preserves literal straight-alpha pixels in generic and Switch-native output without changing source bytes.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Png_preserves_rgba_identity_and_independent_ownership(bool native) {
        string path = FixturePath(native ? "native-source.png" : "rgba-source.png");
        byte[] png = Convert.FromBase64String(RgbaPng);
        byte[] expected = [255, 0, 0, 255, 0, 0, 255, 128];
        File.WriteAllBytes(path, png);
        try {
            TextureAssetProcessorSettings settings = Settings(native);
            using TextureAsset first = new SwitchTextureSourceProcessor().Cook(path, "menu-logo", settings, false);
            Assert.Equal(2, first.Width);
            Assert.Equal(1, first.Height);
            Assert.Equal(native ? TextureAssetColorFormat.SwitchNative : TextureAssetColorFormat.Rgba32, first.ColorFormat);
            Assert.Equal(TextureAssetAlphaPrecision.A8, first.AlphaPrecision);
            Assert.Equal("menu-logo", first.Id);
            Assert.Equal(RuntimeAssetIdGenerator.Generate("menu-logo"), first.RuntimeAssetId);
            Assert.Equal(expected, TextureAssetPixelCodec.DecodeToRgba32(first));
            using (MemoryStream serialized = new(helengine.files.AssetSerializer.SerializeToBytes(first))) {
                using TextureAsset restored = Assert.IsType<TextureAsset>(helengine.files.AssetSerializer.Deserialize(serialized));
                Assert.Equal(expected, TextureAssetPixelCodec.DecodeToRgba32(restored));
            }
            if (!native) first.Colors[0] = 0;
            using TextureAsset second = new SwitchTextureSourceProcessor().Cook(path, "menu-logo", settings, false);
            Assert.NotSame(first.Colors, second.Colors);
            Assert.Equal(expected, TextureAssetPixelCodec.DecodeToRgba32(second));
            Assert.Equal(png, File.ReadAllBytes(path));
            using FileStream exclusive = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        } finally { File.Delete(path); }
    }

    /// <summary>Imports the checked-in real SFNT face and retains a usable atlas after its owning font is disposed.</summary>
    [Fact]
    public void Raw_font_atlas_uses_source_face_and_remains_owned() {
        string path = Environment.GetEnvironmentVariable("HELENGINE_SWITCH_TEST_FONT")
            ?? throw new InvalidOperationException("A checked-in source font fixture path is required.");
        byte[] original = File.ReadAllBytes(path);
        using TextureAsset first = new SwitchTextureSourceProcessor().Cook(path, "menu-font#atlas", Settings(false), true);
        Assert.Equal("menu-font#atlas", first.Id);
        Assert.Equal(RuntimeAssetIdGenerator.Generate("menu-font#atlas"), first.RuntimeAssetId);
        Assert.True(first.Width > 0 && first.Height > 0);
        byte[] expected = TextureAssetPixelCodec.DecodeToRgba32(first);
        Assert.Contains(expected.Where((value, index) => index % 4 == 3), alpha => alpha != 0);
        first.Colors[0] ^= 255;
        using TextureAsset second = new SwitchTextureSourceProcessor().Cook(path, "menu-font#atlas", Settings(false), true);
        Assert.NotSame(first.Colors, second.Colors);
        Assert.Equal(expected, TextureAssetPixelCodec.DecodeToRgba32(second));
        Assert.Equal(original, File.ReadAllBytes(path));
    }

    /// <summary>Keeps the packaged atlas path independent of raw-font importers and preserves its source payload.</summary>
    [Fact]
    public void Serialized_font_atlas_retains_pixels_after_font_disposal() {
        string path = FixturePath("packaged-source.hefont");
        byte[] expected = [11, 22, 33, 128, 44, 55, 66, 255];
        using FontAsset font = new(new FontInfo("Original source fixture", 16, 4), null, new Dictionary<char, FontChar>(), 16, 2, 1);
        font.SourceTextureAsset = new TextureAsset { Width = 2, Height = 1, Colors = expected.ToArray(), PaletteColors = [], ColorFormat = TextureAssetColorFormat.Rgba32, AlphaPrecision = TextureAssetAlphaPrecision.A8 };
        try {
            using (FileStream file = File.Create(path)) helengine.files.FontAssetBinarySerializer.Serialize(file, font);
            byte[] original = File.ReadAllBytes(path);
            using TextureAsset cooked = new SwitchTextureSourceProcessor().Cook(path, "packaged#atlas", Settings(false), true);
            Assert.Equal(expected, cooked.Colors);
            cooked.Colors[0] = 255;
            Assert.Equal(expected, font.SourceTextureAsset.Colors);
            Assert.Equal(original, File.ReadAllBytes(path));
        } finally { File.Delete(path); }
    }

    /// <summary>Preserves the dedicated BNTX dispatch and exact pixels rather than routing native containers through raster decoding.</summary>
    [Fact]
    public void Native_bntx_dispatch_preserves_source_bytes_and_pixels() {
        string path = SwitchBntxTextureTests.Fixture("rgba-gob.bntx");
        byte[] original = File.ReadAllBytes(path);
        using TextureAsset cooked = new SwitchTextureSourceProcessor().Cook(path, "native-logo", Settings(false), false);
        Assert.Equal(File.ReadAllBytes(SwitchBntxTextureTests.Fixture("rgba-gob.layer0.mip0.rgba")), cooked.Colors);
        Assert.Equal(RuntimeAssetIdGenerator.Generate("native-logo"), cooked.RuntimeAssetId);
        Assert.Equal(original, File.ReadAllBytes(path));
    }

    /// <summary>Releases the source stream after a decoder failure and never rewrites malformed authoring data.</summary>
    [Theory]
    [InlineData(".png", false)]
    [InlineData(".ttf", true)]
    public void Invalid_source_failure_releases_file_ownership(string extension, bool fontAtlas) {
        string path = FixturePath("malformed-source" + extension);
        byte[] original = [1, 2, 3];
        File.WriteAllBytes(path, original);
        try {
            Assert.ThrowsAny<Exception>(() => new SwitchTextureSourceProcessor().Cook(path, "invalid", Settings(false), fontAtlas));
            Assert.Equal(original, File.ReadAllBytes(path));
            using FileStream exclusive = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        } finally { File.Delete(path); }
    }

    /// <summary>Resolves only a fixed caller-owned temporary output root, never the repository or the OS temporary directory.</summary>
    static string FixturePath(string name) {
        string root = Environment.GetEnvironmentVariable("HELENGINE_SWITCH_TEXTURE_TEST_OUTPUT")
            ?? throw new InvalidOperationException("A fixed Switch texture test output root is required.");
        Directory.CreateDirectory(root);
        return Path.Combine(root, name);
    }

    /// <summary>Selects lossless generic or native RGBA8 storage while preserving all eight alpha bits.</summary>
    static TextureAssetProcessorSettings Settings(bool native) => new() {
        ColorFormatId = native ? "Switch.Linear.RGBA8_Unorm" : "Rgba32",
        AlphaPrecision = TextureAssetAlphaPrecision.A8
    };
}
