using helengine;
using helengine.editor;
using helengine.baseplatform.Manifest;
using helengine.baseplatform.Builders;
using helengine.baseplatform.Profiles;
using helengine.baseplatform.Reporting;
using helengine.baseplatform.Requests;
using helengine.baseplatform.Targets;
using helengine.baseplatform.Definitions;

namespace helengine.switch_platform.builder.tests;

/// <summary>Exercises real Switch texture/font cooking and RomFS orchestration while substituting only the final NRO compiler.</summary>
public sealed class SwitchTextureBuildTests : ISwitchNativeBuildExecutor, IPlatformBuildProgressReporter, IPlatformBuildDiagnosticReporter {
    /// <summary>Records the resolved workspace after cooking and staging have completed.</summary>
    public SwitchBuildWorkspace Workspace { get; private set; }
    /// <summary>Checks image and packaged-font atlas work items reach RomFS with stable identities and owned native buffers.</summary>
    /// <param name="fontAtlas">Whether the source supplies a font atlas rather than a texture.</param>
    /// <param name="storageBits">Target numeric width required by the packaged runtime reader.</param>
    [Theory]
    [InlineData(false, 32)]
    [InlineData(true, 32)]
    [InlineData(false, 64)]
    [InlineData(true, 64)]
    public async Task Builder_cooks_and_stages_native_texture_work_items(bool fontAtlas, int storageBits) {
        NumericPrecisionPolicy numericPolicy = new(storageBits, storageBits);
        string fixtureRoot = Environment.GetEnvironmentVariable("HELENGINE_SWITCH_TEXTURE_TEST_OUTPUT")
            ?? throw new InvalidOperationException("A repository-local texture test output directory is required.");
        string root = Path.Combine(fixtureRoot, "cooker-policy-test");
        string working = Path.Combine(root, "working"), generated = Path.Combine(root, "generated-core");
        Directory.CreateDirectory(generated);
        File.WriteAllText(Path.Combine(generated, "helcpp_config.hpp"), "#define HE_CPP_PLATFORM_SWITCH 1");
        File.WriteAllText(Path.Combine(generated, "helengine_core_unity.cpp"), "// Original orchestration-test declaration fixture.");
        string source = SwitchBntxTextureTests.Fixture("rgba-gob.bntx");
        string relative = "cooked/textures/native.hetex";
        PlatformBuildArtifact[] artifacts = [];
        if (fontAtlas) {
            // The staged font is authoritative; the original source path is deliberately absent.
            string fontPath = Path.Combine(working, "package-source", "cooked", "textures", "native.hefont");
            Directory.CreateDirectory(Path.GetDirectoryName(fontPath));
            using FontAsset font = new FontAsset(new FontInfo("Original fixture", 5, 2), null, new Dictionary<char, FontChar>(), 5, 5, 5);
            font.SourceTextureAsset = SwitchNativeTextureTests.Pattern(5, 5);
            using FileStream stream = File.Create(fontPath); helengine.files.FontAssetBinarySerializer.Serialize(stream, font);
            source = Path.Combine(root, "missing-original.ttf");
            artifacts = [new PlatformBuildArtifact("cooked/textures/native.hefont", "font", "hash", "runtime-font", "switch")];
        }
        var settings = new TextureAssetProcessorSettings { ColorFormatId = "Switch.BlockLinear.RGBA_BC7_Unorm", AlphaPrecision = TextureAssetAlphaPrecision.A8 };
        PlatformCookWorkItem item = new PlatformCookWorkItem("native", source, fontAtlas ? "font-atlas-texture" : "texture", "switch", "runtime-texture",
            relative, "native", "source-hash", "settings-hash", SwitchTextureCookSettingsSerializer.Serialize(settings),
            [new PlatformCookWorkItemMetadata("source-asset-id", "fixture-texture")]);
        string scenePath = Path.Combine(working, "package-source", "cooked", "scenes", "startup.hasset");
        Directory.CreateDirectory(Path.GetDirectoryName(scenePath)); File.WriteAllText(scenePath, "original staging fixture");
        PlatformBuildScene scene = new PlatformBuildScene("startup", "Startup", "Scenes/Startup.helen",
            [new PlatformBuildPayloadReference("scene", "cooked/scenes/startup.hasset")],
            [new KeyValuePair<string, string>(PlatformBuildSceneMetadataKeys.CookedRelativePath, "cooked/scenes/startup.hasset")]);
        PlatformBuildManifest manifest = new PlatformBuildManifest(1, "project", "1.0.0", "1.0.0", "switch", "1.0.0", "startup",
            [scene], [], artifacts, [], [], new PlatformContainerWritePlan(string.Empty, []), [item], PlatformBuildRuntimeFeatureManifest.Empty);
        PlatformBuildRequest request = new PlatformBuildRequest(manifest,
            [new PlatformBuildTargetVariant("switch-default", "switch", "switch", "debug")],
            [new PlatformCookProfile("debug", "Debug", new PlatformCookProfileCapabilities("switch", "raw", "rgba", "switch-scene-v1", PlatformSerializationEndianness.LittleEndian))],
            Path.Combine(root, "output"), working, "debug", "switch-native", "default", new Dictionary<string, string>(), new Dictionary<string, string>(),
            new Dictionary<string, string> {
                [PlatformCodegenSettingIds.RealStorageBits] = storageBits.ToString(),
                [PlatformCodegenSettingIds.RealArithmeticBits] = storageBits.ToString()
            }, generated, "switch-homebrew-nro", "romfs-package");
        var report = await new SwitchPlatformAssetBuilder(this, root).BuildAsync(request, this, this, CancellationToken.None);
        Assert.True(report.Succeeded); Assert.NotNull(Workspace);
        string cookedPath = Path.Combine(working, "package-source", relative);
        Assert.Equal(File.ReadAllBytes(cookedPath), File.ReadAllBytes(Path.Combine(working, "romfs", relative)));
        using (FileStream cookedStream = File.OpenRead(cookedPath)) {
            using TextureAsset native = Assert.IsType<TextureAsset>(PackagedAssetBinarySerializer.Deserialize(cookedStream, numericPolicy));
            Assert.Equal(TextureAssetColorFormat.SwitchNative, native.ColorFormat);
            Assert.Equal(RuntimeAssetIdGenerator.Generate("fixture-texture" + (fontAtlas ? "#atlas" : "")), native.RuntimeAssetId);
            Assert.Equal(fontAtlas ? 5 : 13, native.Width); Assert.Equal(fontAtlas ? 5 : 9, native.Height);
            Assert.Equal(native.Width * native.Height * 4, TextureAssetPixelCodec.DecodeToRgba32(native).Length);
        }
        if (fontAtlas) Assert.True(File.Exists(Path.Combine(working, "romfs", "cooked/textures/native.hefont")));
        Directory.Delete(root, recursive: true);
    }
    /// <summary>Checks material payloads follow the target policy rather than the managed tooling host.</summary>
    /// <param name="storageBits">Target storage and arithmetic width required by the runtime reader.</param>
    [Theory]
    [InlineData(32)]
    [InlineData(64)]
    public void Material_cook_preserves_target_numeric_policy(int storageBits) {
        NumericPrecisionPolicy numericPolicy = new(storageBits, storageBits);
        var result = new SwitchPlatformAssetBuilder().CookMaterial(new PlatformMaterialCookRequest(
            "menu-material", "assets/menu.hasset", "switch", "debug", "switch-native",
            SwitchMaterialSchemaIds.StandardTexturedSchemaId, new Dictionary<string, string> {
                [SwitchMaterialSchemaIds.TextureRelativePathFieldId] = "cooked/textures/menu.hetex",
                [SwitchMaterialSchemaIds.DoubleSidedFieldId] = "false",
                [SwitchMaterialSchemaIds.VertexColorModeFieldId] = "multiply",
                [SwitchMaterialSchemaIds.BaseColorFieldId] = "#FFFFFFFF",
                [SwitchMaterialSchemaIds.LightingModeFieldId] = "unlit"
            }, numericPolicy: numericPolicy));
        using MemoryStream stream = new(result.CookedMaterialBytes);
        PlatformMaterialAsset material = Assert.IsType<PlatformMaterialAsset>(PackagedAssetBinarySerializer.Deserialize(stream, numericPolicy));
        Assert.Equal(RuntimeAssetIdGenerator.Generate("menu-material"), material.RuntimeAssetId);
        Assert.Equal("cooked/textures/menu.hetex", material.TextureRelativePath);
    }
    /// <summary>Rejects output traversal before opening a source or creating an output file.</summary>
    [Fact]
    public void Work_item_cooker_rejects_output_escape() {
        var settings = new TextureAssetProcessorSettings { ColorFormatId = "Switch.Linear.RGBA8_Unorm", AlphaPrecision = TextureAssetAlphaPrecision.A8 };
        PlatformCookWorkItem item = new PlatformCookWorkItem("escape", SwitchBntxTextureTests.Fixture("rgba-linear.bntx"), "texture", "switch", "runtime-texture",
            "../escaped.hetex", "escape", "hash", "hash", SwitchTextureCookSettingsSerializer.Serialize(settings), []);
        Assert.Throws<InvalidOperationException>(() => new SwitchTextureWorkItemCooker().Cook(item, AppContext.BaseDirectory));
    }
    /// <summary>Captures the final workspace and writes an explicit stub NRO after the production cook and staging steps.</summary>
    void ISwitchNativeBuildExecutor.Build(SwitchBuildWorkspace workspace, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested(); Workspace = workspace;
        Directory.CreateDirectory(Path.GetDirectoryName(workspace.RepositoryPackagePath)); File.WriteAllText(workspace.RepositoryPackagePath, "orchestration-test NRO stub");
    }
    /// <summary>Accepts production progress reports; file assertions verify the resulting build behavior.</summary>
    void IPlatformBuildProgressReporter.Report(PlatformBuildProgressUpdate update) { Assert.NotNull(update); }
    /// <summary>Fails immediately if the production orchestration emits any unexpected diagnostic.</summary>
    void IPlatformBuildDiagnosticReporter.Report(PlatformBuildDiagnostic diagnostic) { Assert.Fail(diagnostic.ToString()); }
}
