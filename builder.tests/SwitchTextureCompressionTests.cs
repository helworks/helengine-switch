using helengine;
using helengine.editor;
using AssetRipper.TextureDecoder.Bc;
using AssetRipper.TextureDecoder.Rgb.Formats;

namespace helengine.switch_platform.builder.tests;

/// <summary>Checks compressed storage against an independent decoder and literal normalized EAC and signed BC values.</summary>
public sealed class SwitchTextureCompressionTests {
    /// <summary>Enumerates BC1/2/3/7 and all ASTC footprints without duplicate layouts or transfer functions.</summary>
    public static IEnumerable<object[]> Formats() {
        foreach (SwitchTextureFormat format in SwitchTextureFormatCatalog.Formats) {
            if (format.BlockLinear || format.Name.EndsWith("sRGB", StringComparison.Ordinal)) continue;
            if (format.Name.Contains("ASTC", StringComparison.Ordinal) || format.Name == "RGBA_BC1" || format.Name == "RGBA_BC2"
                || format.Name == "RGBA_BC3" || format.Name == "RGBA_BC7_Unorm") yield return new object[] { format.Id };
        }
    }
    /// <summary>Confirms actual compressed output agrees with a second decoder within one byte of interpolation rounding.</summary>
    [Theory]
    [MemberData(nameof(Formats))]
    public void Encoded_blocks_agree_with_independent_decoder(string id) {
        SwitchTextureFormatCatalog.TryGetFormat(id, out SwitchTextureFormat format);
        using TextureAsset source = SwitchNativeTextureTests.Pattern(13, 9);
        byte[] tight = SwitchTextureCompression.Encode(source.Colors, 13, 9, format);
        byte[] expected = new byte[13 * 9 * 4];
        if (format.Name.Contains("ASTC", StringComparison.Ordinal)) AssetRipper.TextureDecoder.Astc.AstcDecoder.DecodeASTC<ColorRGBA<byte>, byte>(tight, 13, 9, format.BlockWidth, format.BlockHeight, expected);
        else if (format.Name == "RGBA_BC1") Bc1.Decompress<ColorRGBA<byte>, byte>(tight, 13, 9, expected);
        else if (format.Name == "RGBA_BC2") Bc2.Decompress<ColorRGBA<byte>, byte>(tight, 13, 9, expected);
        else if (format.Name == "RGBA_BC3") Bc3.Decompress<ColorRGBA<byte>, byte>(tight, 13, 9, expected);
        else Bc7.Decompress<ColorRGBA<byte>, byte>(tight, 13, 9, expected);
        byte[] actual = SwitchTextureCompression.Decode(tight, 13, 9, format);
        for (int index = 0; index < actual.Length; index++) Assert.InRange(Math.Abs(actual[index] - expected[index]), 0, 1);
    }
    /// <summary>Checks multiplier-zero precision and signed normalization rather than a biased EAC color view.</summary>
    [Fact]
    public void Eac_and_signed_bc_have_literal_normalized_values() {
        byte[] eac = new byte[8]; eac[0] = 127;
        Assert.Equal(new byte[] { 253, 0, 0, 255 }, SwitchEacTextureCodec.Decode(eac, 4, 4, true, false).Take(4));
        eac[0] = unchecked((byte)-127);
        Assert.Equal(new byte[] { 0, 0, 0, 255 }, SwitchEacTextureCodec.Decode(eac, 4, 4, true, false).Take(4));
        eac[0] = 128; Assert.Throws<InvalidDataException>(() => SwitchEacTextureCodec.Decode(eac, 4, 4, true, false));
        eac[0] = 255; eac[1] = 0x01; // Table 1, multiplier zero, selector 0: 2040+4-3 = 2041.
        Assert.Equal(254, SwitchEacTextureCodec.Decode(eac, 4, 4, false, false)[0]);
        SwitchTextureFormatCatalog.TryGetFormat("Switch.Linear.R_BC4_Snorm", out SwitchTextureFormat bc);
        byte[] endpoints = new byte[8]; endpoints[0] = 127; endpoints[1] = unchecked((byte)-127);
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, SwitchTextureCompression.Decode(endpoints, 4, 4, bc).Take(4));
        for (int index = 2; index < 8; index++) endpoints[index] = 0xff;
        Assert.Equal(new byte[] { 0, 0, 0, 255 }, SwitchTextureCompression.Decode(endpoints, 4, 4, bc).Take(4));
    }
}
