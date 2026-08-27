using helengine.baseplatform.Manifest;
using helengine.switch_platform.builder;

namespace helengine.switch_platform.builder.tests;

/// <summary>
/// Verifies the Switch RomFS stager copies referenced payloads into the staged package root.
/// </summary>
public sealed class SwitchRomFsAssetStagerTests : IDisposable {
    /// <summary>
    /// Temporary test root.
    /// </summary>
    readonly string RootPath;

    /// <summary>
    /// Initializes the temporary test workspace.
    /// </summary>
    public SwitchRomFsAssetStagerTests() {
        RootPath = Path.Combine(Path.GetTempPath(), "helengine-switch-romfs-stager-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
    }

    /// <summary>
    /// Cleans up the temporary test workspace.
    /// </summary>
    public void Dispose() {
        if (Directory.Exists(RootPath)) {
            Directory.Delete(RootPath, true);
        }
    }

    /// <summary>
    /// Ensures scene and loose-asset payloads are copied into the staged RomFS root using their cooked-relative paths.
    /// </summary>
    [Fact]
    public void Stage_copies_scene_and_loose_asset_payloads_into_romfs_root() {
        string sourceRootPath = Path.Combine(RootPath, "package-source");
        string romFsRootPath = Path.Combine(RootPath, "romfs");
        Directory.CreateDirectory(Path.Combine(sourceRootPath, "cooked", "scenes"));
        Directory.CreateDirectory(Path.Combine(sourceRootPath, "cooked", "textures"));
        File.WriteAllText(Path.Combine(sourceRootPath, "cooked", "scenes", "startup.hasset"), "scene");
        File.WriteAllText(Path.Combine(sourceRootPath, "cooked", "textures", "ui.tex"), "texture");

        PlatformBuildManifest manifest = new(
            1,
            "project",
            "1.0.0",
            "1.0.0",
            "startup",
            [
                new PlatformBuildScene(
                    "startup",
                    "Startup",
                    "Scenes/Startup.helen",
                    [new PlatformBuildPayloadReference("scene-payload", "cooked/scenes/startup.hasset")],
                    [new KeyValuePair<string, string>(PlatformBuildSceneMetadataKeys.CookedRelativePath, "cooked/scenes/startup.hasset")])
            ],
            [
                new PlatformBuildAsset(
                    "ui",
                    "UiTexture",
                    "Textures/ui.png",
                    new PlatformBuildPayloadReference("texture-payload", "cooked/textures/ui.tex"),
                    [])
            ],
            [],
            [],
            [],
            new PlatformContainerWritePlan(string.Empty, []),
            Array.Empty<PlatformCookWorkItem>(),
            PlatformBuildRuntimeFeatureManifest.Empty);

        SwitchRomFsAssetStager stager = new();
        stager.Stage(manifest, sourceRootPath, romFsRootPath);

        Assert.Equal("scene", File.ReadAllText(Path.Combine(romFsRootPath, "cooked", "scenes", "startup.hasset")));
        Assert.Equal("texture", File.ReadAllText(Path.Combine(romFsRootPath, "cooked", "textures", "ui.tex")));
    }
}
