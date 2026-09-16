using helengine.baseplatform.Definitions;
using helengine.baseplatform.Builders;
using helengine.switch_platform.builder;
using helengine.baseplatform.Manifest;
using helengine.baseplatform.Profiles;
using helengine.baseplatform.Reporting;
using helengine.baseplatform.Requests;
using helengine.baseplatform.Targets;

namespace helengine.switch_platform.builder.tests;

/// <summary>
/// Verifies the Switch builder publishes the expected platform metadata.
/// </summary>
public sealed class SwitchPlatformAssetBuilderTests {
    /// <summary>
    /// Ensures the native Switch boot host receives the generated host filesystem source it references outside managed reachability analysis.
    /// </summary>
    [Fact]
    public void Definition_force_enables_host_file_system_runtime_feature() {
        SwitchPlatformAssetBuilder builder = new();
        PlatformCodegenProfileDefinition codegenProfile = Assert.Single(
            builder.Definition.CodegenProfiles,
            profile => profile.ProfileId == "default");
        PlatformSettingDefinition enabledFeatures = Assert.Single(
            codegenProfile.Settings,
            setting => setting.SettingId == PlatformCodegenSettingIds.EnabledFeatures);

        Assert.Equal("host_file_system", enabledFeatures.DefaultValue);
    }

    /// <summary>
    /// Ensures the Switch builder descriptor and definition expose the expected identity and runtime packaging metadata.
    /// </summary>
    [Fact]
    public void Descriptor_and_definition_expose_switch_metadata() {
        SwitchPlatformAssetBuilder builder = new();

        Assert.Equal("helengine.switch.builder", builder.Descriptor.BuilderId);
        Assert.Equal("switch", builder.Descriptor.TargetPlatformId);
        Assert.Equal("switch", builder.Definition.PlatformId);
        Assert.Contains(builder.Definition.BuildProfiles, profile => profile.ProfileId == "debug");
        Assert.Contains(builder.Definition.GraphicsProfiles, profile => profile.ProfileId == "switch-native");
        Assert.Contains(builder.Definition.StorageProfiles, profile =>
            profile.ProfileId == "romfs-package" &&
            profile.RuntimeSpecializationId == "switch-romfs-package");

        Assert.Contains(builder.Definition.CodegenProfiles, profile => profile.ProfileId == "default");
        Assert.Contains(builder.Definition.MediaProfiles, profile => profile.ProfileId == "switch-homebrew-nro");

        PlatformBuildProfileDefinition buildProfile = Assert.Single(
            builder.Definition.BuildProfiles,
            profile => profile.ProfileId == "debug");
        PlatformCodegenProfileDefinition codegenProfile = Assert.Single(
            builder.Definition.CodegenProfiles,
            profile => profile.ProfileId == "default");
        Assert.Contains(buildProfile.Settings, setting => setting.SettingId == "startup-clear-color");
        Assert.Contains(codegenProfile.Settings, setting => setting.SettingId == "generated-math-convention");
        Assert.Contains(codegenProfile.Settings, setting => setting.SettingId == "pointer-size-bytes");

        Assert.Contains(builder.Definition.ComponentSupportRules, supportRule =>
            supportRule.ComponentTypeId == "helengine.meshcomponent" &&
            supportRule.SupportKind == PlatformComponentSupportKind.PassThrough);
    }

    /// <summary>
    /// Ensures the Switch builder stages RomFS payloads, writes generated runtime manifests, and exports the built NRO.
    /// </summary>
    [Fact]
    public async Task BuildAsync_stages_romfs_payloads_and_exports_the_native_package() {
        string rootPath = Path.Combine(Path.GetTempPath(), "helengine-switch-builder-tests", Guid.NewGuid().ToString("N"));
        string workingRoot = Path.Combine(rootPath, "working");
        string outputRoot = Path.Combine(rootPath, "output");
        string generatedCoreRoot = Path.Combine(rootPath, "generated-core");
        string packageSourceRoot = Path.Combine(workingRoot, "package-source");
        try {
            Directory.CreateDirectory(Path.Combine(packageSourceRoot, "cooked", "scenes"));
            Directory.CreateDirectory(generatedCoreRoot);
            File.WriteAllText(Path.Combine(packageSourceRoot, "cooked", "scenes", "startup.hasset"), "scene");
            File.WriteAllText(Path.Combine(generatedCoreRoot, "helcpp_config.hpp"), "#define HE_CPP_PLATFORM_SWITCH 1");
            File.WriteAllText(Path.Combine(generatedCoreRoot, "helengine_core_unity.cpp"), "// unity");

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
                new PlatformContainerWritePlan(string.Empty, []),
                Array.Empty<PlatformCookWorkItem>(),
                PlatformBuildRuntimeFeatureManifest.Empty);

            PlatformBuildRequest request = new(
                manifest,
                [new PlatformBuildTargetVariant("switch-default", "switch", "switch", "debug")],
                [
                    new PlatformCookProfile(
                        "debug",
                        "Debug",
                        new PlatformCookProfileCapabilities(
                            "switch",
                            "raw",
                            "rgba",
                            "switch-scene-v1",
                            PlatformSerializationEndianness.LittleEndian))
                ],
                outputRoot,
                workingRoot,
                "debug",
                "switch-native",
                "default",
                new Dictionary<string, string> {
                    ["startup-clear-color"] = "#FFFF00"
                },
                new Dictionary<string, string> {
                    ["default-width"] = "1280",
                    ["default-height"] = "720",
                    ["vsync-enabled"] = "true"
                },
                new Dictionary<string, string> {
                    ["load-native-runtime-metadata"] = "true"
                },
                generatedCoreRoot,
                "switch-homebrew-nro",
                "romfs-package");

            RecordingSwitchNativeBuildExecutor nativeBuildExecutor = new();
            SwitchPlatformAssetBuilder builder = new(nativeBuildExecutor, rootPath);
            RecordingProgressReporter progressReporter = new();
            RecordingDiagnosticReporter diagnosticReporter = new();

            PlatformBuildReport report = await builder.BuildAsync(request, progressReporter, diagnosticReporter, CancellationToken.None);

            Assert.True(report.Succeeded);
            Assert.Empty(diagnosticReporter.Diagnostics);
            Assert.NotNull(nativeBuildExecutor.Workspace);
            Assert.True(File.Exists(Path.Combine(outputRoot, "helengine_switch.nro")));
            Assert.True(File.Exists(Path.Combine(workingRoot, "romfs", "cooked", "scenes", "startup.hasset")));
            Assert.True(File.Exists(Path.Combine(generatedCoreRoot, "runtime", "runtime_startup_manifest.cpp")));
            Assert.True(progressReporter.Updates.Count >= 2);
        } finally {
            if (Directory.Exists(rootPath)) {
                Directory.Delete(rootPath, true);
            }
        }
    }

    /// <summary>
    /// Ensures the Switch builder can export one engine-only bootstrap package when no generated core or staged content exists.
    /// </summary>
    [Fact]
    public async Task BuildAsync_without_generated_core_or_package_source_exports_bootstrap_package() {
        string rootPath = Path.Combine(Path.GetTempPath(), "helengine-switch-builder-tests", Guid.NewGuid().ToString("N"));
        string workingRoot = Path.Combine(rootPath, "working");
        string outputRoot = Path.Combine(rootPath, "output");
        try {
            Directory.CreateDirectory(workingRoot);

            PlatformBuildManifest manifest = new(
                1,
                "project",
                "1.0.0",
                "1.0.0",
                "switch",
                "1.0.0",
                string.Empty,
                Array.Empty<PlatformBuildScene>(),
                Array.Empty<PlatformBuildAsset>(),
                Array.Empty<PlatformBuildArtifact>(),
                Array.Empty<PlatformBuildCodeModule>(),
                Array.Empty<PlatformArtifactPlacement>(),
                new PlatformContainerWritePlan(string.Empty, []),
                Array.Empty<PlatformCookWorkItem>(),
                PlatformBuildRuntimeFeatureManifest.Empty);

            PlatformBuildRequest request = new(
                manifest,
                [new PlatformBuildTargetVariant("switch-default", "switch", "switch", "debug")],
                [
                    new PlatformCookProfile(
                        "debug",
                        "Debug",
                        new PlatformCookProfileCapabilities(
                            "switch",
                            "raw",
                            "rgba",
                            "switch-scene-v1",
                            PlatformSerializationEndianness.LittleEndian))
                ],
                outputRoot,
                workingRoot,
                "debug",
                "switch-native",
                "default",
                new Dictionary<string, string> {
                    ["startup-clear-color"] = "#FFFF00"
                },
                new Dictionary<string, string> {
                    ["default-width"] = "1280",
                    ["default-height"] = "720",
                    ["vsync-enabled"] = "true"
                },
                new Dictionary<string, string> {
                    ["load-native-runtime-metadata"] = "true"
                },
                string.Empty,
                "switch-homebrew-nro",
                "romfs-package");

            RecordingSwitchNativeBuildExecutor nativeBuildExecutor = new();
            SwitchPlatformAssetBuilder builder = new(nativeBuildExecutor, rootPath);
            RecordingProgressReporter progressReporter = new();
            RecordingDiagnosticReporter diagnosticReporter = new();

            PlatformBuildReport report = await builder.BuildAsync(request, progressReporter, diagnosticReporter, CancellationToken.None);

            Assert.True(report.Succeeded);
            Assert.Empty(diagnosticReporter.Diagnostics);
            Assert.NotNull(nativeBuildExecutor.Workspace);
            Assert.Equal(string.Empty, nativeBuildExecutor.Workspace.GeneratedCoreRootPath);
            Assert.Equal(string.Empty, nativeBuildExecutor.Workspace.ContainerGeneratedCoreRootPath);
            Assert.True(File.Exists(Path.Combine(outputRoot, "helengine_switch.nro")));
            Assert.True(Directory.Exists(Path.Combine(workingRoot, "romfs")));
            Assert.DoesNotContain(progressReporter.Updates, update => string.Equals(update.CurrentItemIdentity, "runtime", StringComparison.Ordinal));
            Assert.True(progressReporter.Updates.Count >= 2);
        } finally {
            if (Directory.Exists(rootPath)) {
                Directory.Delete(rootPath, true);
            }
        }
    }

    /// <summary>
    /// Captures the Switch build workspace and emits a stub repository package file for orchestration tests.
    /// </summary>
    sealed class RecordingSwitchNativeBuildExecutor : ISwitchNativeBuildExecutor {
        /// <summary>
        /// Gets the last workspace passed to the fake executor.
        /// </summary>
        public SwitchBuildWorkspace Workspace { get; private set; }

        /// <summary>
        /// Captures the workspace and writes a stub repository-local package file.
        /// </summary>
        /// <param name="workspace">Resolved build workspace under test.</param>
        /// <param name="cancellationToken">Cancellation token forwarded by the caller.</param>
        public void Build(SwitchBuildWorkspace workspace, CancellationToken cancellationToken) {
            Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            string repositoryPackageDirectoryPath = Path.GetDirectoryName(workspace.RepositoryPackagePath)
                ?? throw new InvalidOperationException("Unable to resolve the Switch repository package directory.");
            Directory.CreateDirectory(repositoryPackageDirectoryPath);
            File.WriteAllText(workspace.RepositoryPackagePath, "switch");
        }
    }

    /// <summary>
    /// Records streamed progress updates for builder tests.
    /// </summary>
    sealed class RecordingProgressReporter : IPlatformBuildProgressReporter {
        /// <summary>
        /// Gets the streamed progress updates.
        /// </summary>
        public List<PlatformBuildProgressUpdate> Updates { get; } = [];

        /// <summary>
        /// Captures one progress update.
        /// </summary>
        /// <param name="update">Update to capture.</param>
        public void Report(PlatformBuildProgressUpdate update) {
            Updates.Add(update);
        }
    }

    /// <summary>
    /// Records streamed diagnostics for builder tests.
    /// </summary>
    sealed class RecordingDiagnosticReporter : IPlatformBuildDiagnosticReporter {
        /// <summary>
        /// Gets the streamed diagnostics.
        /// </summary>
        public List<PlatformBuildDiagnostic> Diagnostics { get; } = [];

        /// <summary>
        /// Captures one diagnostic.
        /// </summary>
        /// <param name="diagnostic">Diagnostic to capture.</param>
        public void Report(PlatformBuildDiagnostic diagnostic) {
            Diagnostics.Add(diagnostic);
        }
    }
}
