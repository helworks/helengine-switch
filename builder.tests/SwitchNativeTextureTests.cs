using helengine.editor;
using helengine;

namespace helengine.switch_platform.builder.tests;

/// <summary>Checks every native Switch format/layout cook, exact wire storage, ownership, alpha and invalid-input handling.</summary>
public sealed class SwitchNativeTextureTests {
    /// <summary>Enumerates intrinsic and opaque coverage without duplicating opaque-only format cases.</summary>
    public static IEnumerable<object[]> Formats() {
        foreach (SwitchTextureFormat format in SwitchTextureFormatCatalog.Formats) {
            yield return new object[] { format.Id, format.Alpha };
            if (format.Alpha != TextureAssetAlphaPrecision.Opaque) yield return new object[] { format.Id, TextureAssetAlphaPrecision.Opaque };
        }
    }
    /// <summary>Exercises the actual encoder, native layout, immutable preview cache and both packaged texture readers.</summary>
    [Theory]
    [MemberData(nameof(Formats))]
    public void All_formats_cook_and_roundtrip(string id, TextureAssetAlphaPrecision alpha) {
        using TextureAsset source = Pattern(5, 5);
        var settings = new TextureAssetProcessorSettings { ColorFormatId = id, AlphaPrecision = alpha };
        Assert.Equal(id, SwitchTextureCookSettingsSerializer.Deserialize(SwitchTextureCookSettingsSerializer.Serialize(settings)).ColorFormatId);
        using TextureAsset native = SwitchNativeTextureCooker.Apply(source, settings);
        SwitchTextureLayout layout = SwitchTextureCodec.ReadLayout(native);
        Assert.Equal(5, native.Width); Assert.Equal(5, native.Height); Assert.Equal(source.Id, native.Id); Assert.Equal(source.RuntimeAssetId, native.RuntimeAssetId);
        byte[] rgba = TextureAssetPixelCodec.DecodeToRgba32(native); Assert.Equal(100, rgba.Length);
        if (alpha == TextureAssetAlphaPrecision.Opaque) for (int index = 3; index < rgba.Length; index += 4) Assert.Equal(255, rgba[index]);
        byte[] nativeBytes = native.Colors.Skip(SwitchTextureCodec.HeaderLength).ToArray();
        byte[] tight = SwitchNativeTextureCooker.ToTight(nativeBytes, layout);
        Assert.Equal(rgba, SwitchNativeTextureCooker.DecodeTight(tight, 5, 5, layout.Format, SwitchTextureCodec.IdentitySwizzle));
        Assert.Equal(nativeBytes, SwitchNativeTextureCooker.FromTight(tight, layout));
        Assert.True(TextureVramUsageCalculator.TryCalculateBytes(5, 5, settings, out long usage)); Assert.Equal(layout.TexelBytes, usage);
        using MemoryStream serialized = new MemoryStream(helengine.files.AssetSerializer.SerializeToBytes(native));
        using TextureAsset file = Assert.IsType<TextureAsset>(helengine.files.AssetSerializer.Deserialize(serialized)); Assert.Equal(rgba, TextureAssetPixelCodec.DecodeToRgba32(file));
        serialized.Position = 0;
        using TextureAsset runtime = Assert.IsType<TextureAsset>(PackagedAssetBinarySerializer.Deserialize(serialized)); Assert.Equal(native.Colors, runtime.Colors); Assert.Equal(rgba, TextureAssetPixelCodec.DecodeToRgba32(runtime));
        Assert.NotSame(source.Colors, native.Colors); Assert.NotSame(rgba, native.PaletteColors);
        if (alpha == layout.Format.Alpha) {
            string root = Environment.GetEnvironmentVariable("HELENGINE_SWITCH_TEXTURE_TEST_OUTPUT");
            if (!string.IsNullOrWhiteSpace(root)) {
                Directory.CreateDirectory(root); string prefix = Path.Combine(root, id);
                File.WriteAllBytes(prefix + ".swt", native.Colors); File.WriteAllBytes(prefix + ".tight", tight); File.WriteAllBytes(prefix + ".rgba", rgba);
            }
        }
    }
    /// <summary>Checks native GOB sectors and packed words against literal physical addresses and channel values.</summary>
    [Fact]
    public void Packed_words_and_gob_addresses_have_literal_layouts() {
        SwitchTextureFormatCatalog.TryGetFormat("Switch.BlockLinear.R8_Unorm", out SwitchTextureFormat format);
        SwitchTextureLayout layout = new SwitchTextureLayout(format, 65, 17, 1);
        Assert.Equal(0, layout.ByteOffset(0, 0)); Assert.Equal(16, layout.ByteOffset(0, 1)); Assert.Equal(32, layout.ByteOffset(16, 0));
        Assert.Equal(64, layout.ByteOffset(0, 2)); Assert.Equal(256, layout.ByteOffset(32, 0)); Assert.Equal(512, layout.ByteOffset(0, 8));
        Assert.Equal(1024, layout.ByteOffset(64, 0)); Assert.Equal(2048, layout.ByteOffset(0, 16));
        using TextureAsset source = Pattern(1, 1); source.Colors = new byte[] { 255, 0, 0, 255 };
        foreach (string name in new[] { "RGB565_Unorm", "RGB5A1_Unorm", "RGBA4_Unorm", "RGB10A2_Unorm", "RGBA16_Float" }) {
            SwitchTextureFormatCatalog.TryGetFormat("Switch.Linear." + name, out SwitchTextureFormat packed);
            byte[] encoded = SwitchTexturePixelCodec.Encode(source.Colors, 1, 1, packed);
            if (name == "RGB565_Unorm") Assert.Equal(new byte[] { 31, 0 }, encoded);
            if (name == "RGB5A1_Unorm") Assert.Equal(new byte[] { 31, 128 }, encoded);
            if (name == "RGBA4_Unorm") Assert.Equal(new byte[] { 15, 240 }, encoded);
            if (name == "RGB10A2_Unorm") Assert.Equal(new byte[] { 255, 3, 0, 192 }, encoded);
            if (name == "RGBA16_Float") Assert.Equal(new byte[] { 0, 60, 0, 0, 0, 0, 0, 60 }, encoded);
            Assert.Equal(source.Colors, SwitchTexturePixelCodec.Decode(encoded, 1, 1, packed));
        }
    }
    /// <summary>Rejects unknown settings, oversized CPU/GPU allocations, invalid mappings and corrupted immutable native or preview buffers.</summary>
    [Fact]
    public void Native_metadata_and_integrity_are_strict() {
        using TextureAsset source = Pattern(5, 5);
        using TextureAsset native = SwitchNativeTextureCooker.Apply(source, new TextureAssetProcessorSettings { ColorFormatId = "Switch.BlockLinear.RGBA8_Unorm", AlphaPrecision = TextureAssetAlphaPrecision.A8 });
        foreach (int offset in new[] { 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 64 }) {
            byte original = native.Colors[offset]; native.Colors[offset] ^= 128;
            Assert.ThrowsAny<ArgumentException>(() => SwitchTextureCodec.ReadLayout(native)); native.Colors[offset] = original;
        }
        native.PaletteColors[0] ^= 128; Assert.Throws<ArgumentException>(() => SwitchTextureCodec.Decode(native));
        Assert.Throws<ArgumentException>(() => SwitchTextureCodec.PreviewBytes(16384, 16384));
        SwitchTextureFormatCatalog.TryGetFormat("Switch.BlockLinear.RGBA32_Float", out SwitchTextureFormat huge);
        Assert.Throws<ArgumentException>(() => new SwitchTextureLayout(huge, 16384, 16384));
        Assert.Throws<ArgumentException>(() => SwitchTextureCookSettingsSerializer.Deserialize("{\"maxResolution\":0,\"colorFormat\":\"Switch.Linear.R8_Unorm\",\"alphaPrecision\":\"A8\"}"));
    }
    /// <summary>Checks complete public catalog coverage and published image/font cook capabilities.</summary>
    [Fact]
    public void Catalog_and_capabilities_cover_all_native_formats() {
        Assert.Equal(258, SwitchTextureFormatCatalog.Formats.Length);
        Assert.Equal(129, SwitchTextureFormatCatalog.Formats.Select(item => item.Code).Distinct().Count());
        foreach (var capability in SwitchPlatformDefinitionFactory.Create().AssetCookCapabilities) Assert.Equal(262, capability.TextureFormatCapabilities.SupportedColorFormatIds.Length);
    }
    /// <summary>Creates an original nonuniform RGBA test image with partial and binary alpha.</summary>
    public static TextureAsset Pattern(int width, int height) {
        byte[] rgba = new byte[width * height * 4];
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) {
            int target = (y * width + x) * 4; rgba[target] = (byte)(x * 47); rgba[target + 1] = (byte)(y * 43); rgba[target + 2] = (byte)((x + y) * 29); rgba[target + 3] = (byte)((x + y) * 31);
        }
        return new TextureAsset { Id = "switch-pattern", RuntimeAssetId = 42, Width = (ushort)width, Height = (ushort)height, ColorFormat = TextureAssetColorFormat.Rgba32, AlphaPrecision = TextureAssetAlphaPrecision.A8, Colors = rgba };
    }
}
