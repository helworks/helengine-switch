using helengine;
using helengine.editor;

namespace helengine.switch_platform.builder.tests;

/// <summary>Verifies original upstream-swizzled BNTX fixtures, subresources, preserved storage and malformed pointer rejection.</summary>
public sealed class SwitchBntxTextureTests {
    /// <summary>Decodes independently generated containers and checks their expected logical pixels and exact native payload bytes.</summary>
    [Theory]
    [InlineData("rgba-linear", 0, 0)]
    [InlineData("rgba-gob", 0, 0)]
    [InlineData("rgba-array-mips", 0, 0)]
    [InlineData("rgba-array-mips", 0, 1)]
    [InlineData("rgba-array-mips", 1, 0)]
    [InlineData("rgba-array-mips", 1, 1)]
    [InlineData("bc1-red", 0, 0)]
    [InlineData("astc-red", 0, 0)]
    public void Imports_native_layout_and_selected_subresource(string name, int layer, int mip) {
        byte[] data = File.ReadAllBytes(Fixture(name + ".bntx"));
        using MemoryStream stream = new MemoryStream(data);
        SwitchBntxTextureImporter importer = new SwitchBntxTextureImporter();
        using TextureAsset native = importer.ImportNativeTexture(stream, 0, layer, mip);
        byte[] expected = File.ReadAllBytes(Fixture(name + ".layer" + layer + ".mip" + mip + ".rgba"));
        Assert.Equal(expected, TextureAssetPixelCodec.DecodeToRgba32(native)); Assert.Equal(name, native.Id);
        SwitchTextureLayout layout = SwitchTextureCodec.ReadLayout(native);
        int baseAddress = (int)SwitchTexturePixelCodec.Read(data, 0x220 + mip * 8, 8);
        int layerStride = (int)SwitchTextureCodec.Read(data, 0x150) / (int)SwitchTextureCodec.Read(data, 0x130);
        Assert.Equal(data.Skip(baseAddress + layer * layerStride).Take(layout.TexelBytes), native.Colors.Skip(SwitchTextureCodec.HeaderLength));
        stream.Position = 0;
        using TextureAsset editor = importer.ImportTexture(stream, 0, layer, mip);
        Assert.Equal(TextureAssetColorFormat.Rgba32, editor.ColorFormat); Assert.Equal(expected, editor.Colors);
        Assert.NotSame(editor.Colors, native.PaletteColors);
    }
    /// <summary>Rejects truncated archives, oversized full-width pointers, invalid dimensions, selectors, formats and selected subresources.</summary>
    [Fact]
    public void Rejects_malformed_containers_and_selections() {
        byte[] valid = File.ReadAllBytes(Fixture("rgba-array-mips.bntx"));
        SwitchBntxTextureImporter importer = new SwitchBntxTextureImporter();
        foreach (int offset in new[] { 0, 12, 15, 28, 32, 39, 47, 95, 0x100, 0x111, 0x11c, 0x127, 0x12f, 0x133, 0x159, 0x177 }) {
            byte[] malformed = (byte[])valid.Clone(); malformed[offset] ^= 128;
            using MemoryStream stream = new MemoryStream(malformed);
            Assert.True(Record.Exception(() => importer.ImportNativeTexture(stream)) != null, "Malformed byte offset: " + offset);
        }
        foreach (int length in new[] { 0, 32, 87, 0x180, valid.Length - 1 }) {
            using MemoryStream stream = new MemoryStream(valid.Take(length).ToArray());
            Assert.Throws<InvalidDataException>(() => importer.ImportNativeTexture(stream));
        }
        using MemoryStream selection = new MemoryStream(valid);
        Assert.Throws<InvalidDataException>(() => importer.ImportNativeTexture(selection, 1)); selection.Position = 0;
        Assert.Throws<InvalidDataException>(() => importer.ImportNativeTexture(selection, 0, 2)); selection.Position = 0;
        Assert.Throws<InvalidDataException>(() => importer.ImportNativeTexture(selection, 0, 0, 2));
    }
    /// <summary>Resolves only checked-in original binary fixtures copied beside the test assembly.</summary>
    public static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", "bntx", name);
}
