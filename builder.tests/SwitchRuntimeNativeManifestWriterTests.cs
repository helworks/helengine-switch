using helengine;
using helengine.baseplatform.Manifest;
using helengine.switch_platform.builder;

namespace helengine.switch_platform.builder.tests;

/// <summary>
/// Verifies the Switch runtime manifest writer emits the generated-core runtime metadata files required by the player.
/// </summary>
public sealed class SwitchRuntimeNativeManifestWriterTests : IDisposable {
    /// <summary>
    /// Temporary test root.
    /// </summary>
    readonly string RootPath;

    /// <summary>
    /// Initializes the temporary test workspace.
    /// </summary>
    public SwitchRuntimeNativeManifestWriterTests() {
        RootPath = Path.Combine(Path.GetTempPath(), "helengine-switch-runtime-manifest-writer-tests", Guid.NewGuid().ToString("N"));
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
    /// Ensures the writer emits startup, scene-catalog, and standard-platform-input manifest sources.
    /// </summary>
    [Fact]
    public void Write_emits_runtime_manifest_sources() {
        string generatedCoreRootPath = Path.Combine(RootPath, "generated-core");
        Directory.CreateDirectory(generatedCoreRootPath);

        PlatformBuildManifest manifest = new(
            1,
            "project",
            "1.0.0",
            "1.0.0",
            "switch",
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
            [],
            [],
            [],
            [],
            new PlatformContainerWritePlan(string.Empty, []));
        manifest.StandardPlatformInputConfiguration = new StandardPlatformInputConfiguration([
            new StandardPlatformActionBinding(
                StandardPlatformAction.Accept,
                new InputControlId(InputDeviceKind.Gamepad, InputControlKind.Button, 0, (int)InputGamepadButton.South))
        ]);

        SwitchRuntimeNativeManifestWriter writer = new();
        writer.Write(generatedCoreRootPath, manifest);

        string runtimeRootPath = Path.Combine(generatedCoreRootPath, "runtime");
        Assert.True(File.Exists(Path.Combine(runtimeRootPath, "runtime_startup_manifest.hpp")));
        Assert.True(File.Exists(Path.Combine(runtimeRootPath, "runtime_startup_manifest.cpp")));
        Assert.True(File.Exists(Path.Combine(runtimeRootPath, "runtime_scene_catalog_manifest.hpp")));
        Assert.True(File.Exists(Path.Combine(runtimeRootPath, "runtime_scene_catalog_manifest.cpp")));
        Assert.True(File.Exists(Path.Combine(runtimeRootPath, "runtime_standard_platform_input_manifest.hpp")));
        Assert.True(File.Exists(Path.Combine(runtimeRootPath, "runtime_standard_platform_input_manifest.cpp")));
    }
}
