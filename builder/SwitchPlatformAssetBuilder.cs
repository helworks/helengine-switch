using helengine.baseplatform.Builders;
using helengine.baseplatform.Definitions;
using helengine.baseplatform.Descriptors;
using helengine.baseplatform.Manifest;
using helengine.baseplatform.Reporting;
using helengine.baseplatform.Requests;
using helengine.baseplatform.Results;

namespace helengine.switch_platform.builder;

/// <summary>
/// Implements the Nintendo Switch platform asset builder contract.
/// </summary>
public sealed class SwitchPlatformAssetBuilder : IPlatformAssetBuilder {
    /// <summary>
    /// Native Switch build executor used to produce the packaged NRO.
    /// </summary>
    readonly ISwitchNativeBuildExecutor NativeBuildExecutor;

    /// <summary>
    /// Writes generated runtime manifest sources consumed by the Switch player.
    /// </summary>
    readonly SwitchRuntimeNativeManifestWriter RuntimeNativeManifestWriter;

    /// <summary>
    /// Stages cooked payloads into the Switch RomFS tree.
    /// </summary>
    readonly SwitchRomFsAssetStager RomFsAssetStager;

    /// <summary>
    /// Optional repository root override used by tests.
    /// </summary>
    readonly string RepositoryRootPath;

    /// <summary>
    /// Initializes one Switch builder instance with the current platform metadata.
    /// </summary>
    public SwitchPlatformAssetBuilder()
        : this(new SwitchNativeBuildExecutor(), string.Empty) {
    }

    /// <summary>
    /// Initializes one Switch builder instance with explicit native-build collaborators.
    /// </summary>
    /// <param name="nativeBuildExecutor">Native build executor used by the Switch builder.</param>
    /// <param name="repositoryRootPath">Optional repository root override used by tests.</param>
    public SwitchPlatformAssetBuilder(ISwitchNativeBuildExecutor nativeBuildExecutor, string repositoryRootPath) {
        NativeBuildExecutor = nativeBuildExecutor ?? throw new ArgumentNullException(nameof(nativeBuildExecutor));
        RuntimeNativeManifestWriter = new SwitchRuntimeNativeManifestWriter();
        RomFsAssetStager = new SwitchRomFsAssetStager();
        RepositoryRootPath = string.IsNullOrWhiteSpace(repositoryRootPath)
            ? string.Empty
            : Path.GetFullPath(repositoryRootPath);
        Descriptor = new PlatformBuilderDescriptor(
            "helengine.switch.builder",
            "1.0.0",
            "switch",
            new EngineCompatibilityRange("1.0.0", "999.0.0"),
            new ManifestCompatibilityRange(1, 3),
            ["switch"],
            ["debug"]);
        Definition = SwitchPlatformDefinitionFactory.Create();
    }

    /// <summary>
    /// Gets the explicit builder descriptor for the Switch builder assembly.
    /// </summary>
    public PlatformBuilderDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the typed Switch platform definition exposed to the editor.
    /// </summary>
    public PlatformDefinition Definition { get; }

    /// <summary>
    /// Cooks one Switch platform-owned material payload for the placeholder native renderer seam.
    /// </summary>
    /// <param name="request">Material cook request to translate.</param>
    /// <returns>Cooked Switch material payload.</returns>
    public PlatformMaterialCookResult CookMaterial(PlatformMaterialCookRequest request) {
        if (request == null) {
            throw new ArgumentNullException(nameof(request));
        } else if (!string.Equals(request.TargetPlatformId, "switch", StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException($"Nintendo Switch cannot cook materials for target platform '{request.TargetPlatformId}'.");
        } else if (!string.Equals(request.SchemaId, SwitchMaterialSchemaIds.StandardTexturedSchemaId, StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException($"Nintendo Switch does not support material schema '{request.SchemaId}'.");
        }

        ResolveBaseColor(
            request.FieldValues,
            out byte baseColorRed,
            out byte baseColorGreen,
            out byte baseColorBlue,
            out byte baseColorAlpha);
        PlatformMaterialAsset cookedAsset = new() {
            Id = request.MaterialAssetId,
            RendererFamilyId = string.IsNullOrWhiteSpace(request.SelectedGraphicsProfileId)
                ? throw new InvalidOperationException("Nintendo Switch material cooking requires a graphics profile id.")
                : request.SelectedGraphicsProfileId,
            TextureRelativePath = ResolveOptionalString(request.FieldValues, SwitchMaterialSchemaIds.TextureRelativePathFieldId),
            DoubleSided = ResolveBoolean(request.FieldValues, SwitchMaterialSchemaIds.DoubleSidedFieldId),
            UseVertexColor = ResolveVertexColorMode(request.FieldValues),
            Lit = ResolveLightingMode(request.FieldValues),
            BaseColorR = baseColorRed,
            BaseColorG = baseColorGreen,
            BaseColorB = baseColorBlue,
            BaseColorA = baseColorAlpha
        };

        return new PlatformMaterialCookResult(
            helengine.files.AssetSerializer.SerializeToBytes(cookedAsset, request.NumericPolicy),
            []);
    }

    /// <summary>
    /// Executes the Switch content build for one fully resolved request.
    /// </summary>
    /// <param name="request">Build request to process.</param>
    /// <param name="progressReporter">Progress reporter that receives streamed updates.</param>
    /// <param name="diagnosticReporter">Diagnostic reporter that receives streamed diagnostics.</param>
    /// <param name="cancellationToken">Cancellation token used to stop the build cooperatively.</param>
    /// <returns>The final Switch build report.</returns>
    public Task<PlatformBuildReport> BuildAsync(
        PlatformBuildRequest request,
        IPlatformBuildProgressReporter progressReporter,
        IPlatformBuildDiagnosticReporter diagnosticReporter,
        CancellationToken cancellationToken) {
        if (request == null) {
            throw new ArgumentNullException(nameof(request));
        } else if (progressReporter == null) {
            throw new ArgumentNullException(nameof(progressReporter));
        } else if (diagnosticReporter == null) {
            throw new ArgumentNullException(nameof(diagnosticReporter));
        }

        bool hasGeneratedCore = !string.IsNullOrWhiteSpace(request.GeneratedCoreCppRootPath);
        bool requiresGeneratedCore = RequiresGeneratedCore(request.Manifest);
        if (!hasGeneratedCore && requiresGeneratedCore) {
            throw new ArgumentException("Generated core root path must be provided for Switch builds that package authored content.", nameof(request));
        } else if (hasGeneratedCore && !Directory.Exists(request.GeneratedCoreCppRootPath)) {
            throw new DirectoryNotFoundException($"Generated core root '{request.GeneratedCoreCppRootPath}' was not found.");
        }

        string repositoryRootPath = string.IsNullOrWhiteSpace(RepositoryRootPath)
            ? SwitchRepositoryRootResolver.ResolveRepositoryRootPath()
            : RepositoryRootPath;
        string packageSourceRootPath = ResolvePackageSourceRootPath(request.WorkingRoot);
        if (request.Manifest.PlatformCookWorkItems.Length > 0) {
            Directory.CreateDirectory(packageSourceRootPath);
            SwitchTextureWorkItemCooker cooker = new SwitchTextureWorkItemCooker();
            foreach (PlatformCookWorkItem workItem in request.Manifest.PlatformCookWorkItems) {
                cancellationToken.ThrowIfCancellationRequested(); cooker.Cook(workItem, packageSourceRootPath, request.NumericPolicy);
            }
        }
        bool hasPackageSource = Directory.Exists(packageSourceRootPath);
        bool requiresPackageSource = RequiresPackageSource(request.Manifest);
        if (!hasPackageSource && requiresPackageSource) {
            throw new DirectoryNotFoundException($"Switch builds require staged package content under '{PackageSourceDirectoryName}' inside the working root.");
        }

        string romFsRootPath = Path.Combine(Path.GetFullPath(request.WorkingRoot), "romfs");
        Directory.CreateDirectory(request.OutputRoot);
        ResetDirectory(romFsRootPath);

        int totalSteps = hasGeneratedCore ? 3 : 2;
        int completedSteps = 0;
        if (hasGeneratedCore) {
            RuntimeNativeManifestWriter.Write(request.GeneratedCoreCppRootPath, request.Manifest);
            completedSteps++;
            progressReporter.Report(new PlatformBuildProgressUpdate(
                "Write Runtime Metadata",
                "runtime",
                completedSteps,
                totalSteps,
                "Wrote the generated runtime manifest sources into the generated-core root."));
        }

        if (hasPackageSource) {
            RomFsAssetStager.Stage(request.Manifest, packageSourceRootPath, romFsRootPath);
        }
        completedSteps++;
        progressReporter.Report(new PlatformBuildProgressUpdate(
            "Stage RomFS Payloads",
            hasPackageSource ? "romfs" : "bootstrap",
            completedSteps,
            totalSteps,
            hasPackageSource
                ? "Staged the cooked payloads into the Switch RomFS root."
                : "No staged package-source content was provided; building the engine-only bootstrap package."));

        SwitchBuildWorkspace workspace = new(
            repositoryRootPath,
            request.WorkingRoot,
            request.OutputRoot,
            hasGeneratedCore ? request.GeneratedCoreCppRootPath : string.Empty,
            romFsRootPath,
            "/workspace-staging/romfs",
            hasGeneratedCore ? "/generated-core" : string.Empty,
            Path.Combine(string.IsNullOrWhiteSpace(request.NativeObjectCacheRoot) ? Path.Combine(repositoryRootPath, "build") : request.NativeObjectCacheRoot, "helengine_switch.nro"),
            Path.Combine(request.OutputRoot, "helengine_switch.nro"),
            request.NativeObjectCacheRoot);
        NativeBuildExecutor.Build(workspace, cancellationToken);
        if (!File.Exists(workspace.RepositoryPackagePath)) {
            throw new FileNotFoundException("The Switch native build did not produce an NRO.", workspace.RepositoryPackagePath);
        }

        File.Copy(workspace.RepositoryPackagePath, workspace.ExportPackagePath, true);
        completedSteps++;
        progressReporter.Report(new PlatformBuildProgressUpdate(
            "Build Switch Package",
            "helengine_switch.nro",
            completedSteps,
            totalSteps,
            "Built and exported the Switch homebrew package."));

        PlatformBuildReport report = new(
            File.Exists(workspace.ExportPackagePath),
            [],
            BuildSceneOutcomes(request.Manifest.Scenes),
            BuildLooseAssetOutcomes(request.Manifest.LooseAssets));
        return Task.FromResult(report);
    }

    /// <summary>
    /// Resolves one required boolean field from the authored material payload.
    /// </summary>
    /// <param name="fieldValues">Authored field values.</param>
    /// <param name="fieldId">Field identifier to resolve.</param>
    /// <returns>Resolved boolean value.</returns>
    static bool ResolveBoolean(IReadOnlyDictionary<string, string> fieldValues, string fieldId) {
        if (fieldValues == null) {
            throw new ArgumentNullException(nameof(fieldValues));
        } else if (string.IsNullOrWhiteSpace(fieldId)) {
            throw new ArgumentException("Field id must be provided.", nameof(fieldId));
        } else if (!fieldValues.TryGetValue(fieldId, out string fieldValue) || string.IsNullOrWhiteSpace(fieldValue)) {
            throw new InvalidOperationException($"The {fieldId} material field is required.");
        } else if (bool.TryParse(fieldValue, out bool parsedValue)) {
            return parsedValue;
        }

        throw new InvalidOperationException($"The {fieldId} material field must be true or false.");
    }

    /// <summary>
    /// Resolves whether the material should use runtime lighting.
    /// </summary>
    /// <param name="fieldValues">Authored field values.</param>
    /// <returns>True when lighting should be enabled.</returns>
    static bool ResolveLightingMode(IReadOnlyDictionary<string, string> fieldValues) {
        string value = ResolveRequiredString(fieldValues, SwitchMaterialSchemaIds.LightingModeFieldId);
        if (string.Equals(value, "lit", StringComparison.OrdinalIgnoreCase)) {
            return true;
        } else if (string.Equals(value, "unlit", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        throw new InvalidOperationException("The lighting-mode material field must be lit or unlit.");
    }

    /// <summary>
    /// Resolves one optional string field from the authored material payload.
    /// </summary>
    /// <param name="fieldValues">Authored field values.</param>
    /// <param name="fieldId">Field identifier to resolve.</param>
    /// <returns>Trimmed field value or an empty string when absent.</returns>
    static string ResolveOptionalString(IReadOnlyDictionary<string, string> fieldValues, string fieldId) {
        if (fieldValues == null) {
            throw new ArgumentNullException(nameof(fieldValues));
        } else if (string.IsNullOrWhiteSpace(fieldId)) {
            throw new ArgumentException("Field id must be provided.", nameof(fieldId));
        }

        return fieldValues.TryGetValue(fieldId, out string value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : string.Empty;
    }

    /// <summary>
    /// Resolves whether vertex colors should modulate the final material color.
    /// </summary>
    /// <param name="fieldValues">Authored field values.</param>
    /// <returns>True when vertex color modulation is enabled.</returns>
    static bool ResolveVertexColorMode(IReadOnlyDictionary<string, string> fieldValues) {
        string value = ResolveRequiredString(fieldValues, SwitchMaterialSchemaIds.VertexColorModeFieldId);
        if (string.Equals(value, "disabled", StringComparison.OrdinalIgnoreCase)) {
            return false;
        } else if (string.Equals(value, "multiply", StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        throw new InvalidOperationException("The vertex-color-mode material field must be disabled or multiply.");
    }

    /// <summary>
    /// Resolves one required string field from the authored material payload.
    /// </summary>
    /// <param name="fieldValues">Authored field values.</param>
    /// <param name="fieldId">Field identifier to resolve.</param>
    /// <returns>Trimmed field value.</returns>
    static string ResolveRequiredString(IReadOnlyDictionary<string, string> fieldValues, string fieldId) {
        string fieldValue = string.Empty;
        if (fieldValues == null) {
            throw new ArgumentNullException(nameof(fieldValues));
        } else if (string.IsNullOrWhiteSpace(fieldId)) {
            throw new ArgumentException("Field id must be provided.", nameof(fieldId));
        } else if (!fieldValues.TryGetValue(fieldId, out fieldValue) || string.IsNullOrWhiteSpace(fieldValue)) {
            throw new InvalidOperationException($"The {fieldId} material field is required.");
        }

        return fieldValue.Trim();
    }

    /// <summary>
    /// Resolves the authored base color into packed channel bytes.
    /// </summary>
    /// <param name="fieldValues">Authored field values.</param>
    /// <param name="red">Resolved red channel.</param>
    /// <param name="green">Resolved green channel.</param>
    /// <param name="blue">Resolved blue channel.</param>
    /// <param name="alpha">Resolved alpha channel.</param>
    static void ResolveBaseColor(
        IReadOnlyDictionary<string, string> fieldValues,
        out byte red,
        out byte green,
        out byte blue,
        out byte alpha) {
        string value = ResolveRequiredString(fieldValues, SwitchMaterialSchemaIds.BaseColorFieldId);
        string normalizedValue = value.StartsWith("#", StringComparison.Ordinal) ? value.Substring(1) : value;
        if (normalizedValue.Length != 6 && normalizedValue.Length != 8) {
            throw new InvalidOperationException("The base-color material field must use #RRGGBB or #RRGGBBAA format.");
        }

        try {
            red = Convert.ToByte(normalizedValue.Substring(0, 2), 16);
            green = Convert.ToByte(normalizedValue.Substring(2, 2), 16);
            blue = Convert.ToByte(normalizedValue.Substring(4, 2), 16);
            alpha = normalizedValue.Length == 8
                ? Convert.ToByte(normalizedValue.Substring(6, 2), 16)
                : (byte)255;
        } catch (FormatException ex) {
            throw new InvalidOperationException("The base-color material field must use #RRGGBB or #RRGGBBAA format.", ex);
        } catch (OverflowException ex) {
            throw new InvalidOperationException("The base-color material field must use #RRGGBB or #RRGGBBAA format.", ex);
        }
    }

    /// <summary>
    /// Stores the working-root child directory that contains the staged package source consumed by the builder.
    /// </summary>
    static string PackageSourceDirectoryName => "package-source";

    /// <summary>
    /// Returns whether the manifest includes authored runtime content that must be merged into generated-core.
    /// </summary>
    /// <param name="manifest">Resolved build manifest to inspect.</param>
    /// <returns>True when generated-core content is required.</returns>
    static bool RequiresGeneratedCore(PlatformBuildManifest manifest) {
        if (manifest == null) {
            throw new ArgumentNullException(nameof(manifest));
        }

        return manifest.Scenes.Length > 0
            || manifest.LooseAssets.Length > 0
            || manifest.CodeModules.Length > 0
            || manifest.CookedArtifacts.Length > 0
            || manifest.ArtifactPlacements.Length > 0
            || manifest.PlatformCookWorkItems.Length > 0;
    }

    /// <summary>
    /// Returns whether the manifest includes packaged content that must already exist under the staged package-source tree.
    /// </summary>
    /// <param name="manifest">Resolved build manifest to inspect.</param>
    /// <returns>True when staged package-source content is required.</returns>
    static bool RequiresPackageSource(PlatformBuildManifest manifest) {
        if (manifest == null) {
            throw new ArgumentNullException(nameof(manifest));
        }

        return manifest.Scenes.Length > 0
            || manifest.LooseAssets.Length > 0
            || manifest.CookedArtifacts.Length > 0
            || manifest.ArtifactPlacements.Length > 0;
    }

    /// <summary>
    /// Builds succeeded outcomes for all scenes in one manifest.
    /// </summary>
    /// <param name="scenes">Scenes included in the request.</param>
    /// <returns>Per-scene succeeded outcomes.</returns>
    static PlatformBuildItemOutcome[] BuildSceneOutcomes(helengine.baseplatform.Manifest.PlatformBuildScene[] scenes) {
        PlatformBuildItemOutcome[] outcomes = new PlatformBuildItemOutcome[scenes.Length];
        for (int index = 0; index < scenes.Length; index++) {
            outcomes[index] = new PlatformBuildItemOutcome(scenes[index].SceneId, PlatformBuildItemOutcomeKind.Succeeded);
        }

        return outcomes;
    }

    /// <summary>
    /// Builds succeeded outcomes for all loose assets in one manifest.
    /// </summary>
    /// <param name="looseAssets">Loose assets included in the request.</param>
    /// <returns>Per-asset succeeded outcomes.</returns>
    static PlatformBuildItemOutcome[] BuildLooseAssetOutcomes(helengine.baseplatform.Manifest.PlatformBuildAsset[] looseAssets) {
        PlatformBuildItemOutcome[] outcomes = new PlatformBuildItemOutcome[looseAssets.Length];
        for (int index = 0; index < looseAssets.Length; index++) {
            outcomes[index] = new PlatformBuildItemOutcome(looseAssets[index].AssetId, PlatformBuildItemOutcomeKind.Succeeded);
        }

        return outcomes;
    }

    /// <summary>
    /// Deletes and recreates one directory.
    /// </summary>
    /// <param name="directoryPath">Directory path to reset.</param>
    static void ResetDirectory(string directoryPath) {
        if (string.IsNullOrWhiteSpace(directoryPath)) {
            throw new ArgumentException("Directory path must be provided.", nameof(directoryPath));
        }

        if (Directory.Exists(directoryPath)) {
            Directory.Delete(directoryPath, true);
        }

        Directory.CreateDirectory(directoryPath);
    }

    /// <summary>
    /// Resolves the builder-owned package source root from the supplied working root.
    /// </summary>
    /// <param name="workingRootPath">Build working root.</param>
    /// <returns>Absolute package source root path.</returns>
    static string ResolvePackageSourceRootPath(string workingRootPath) {
        if (string.IsNullOrWhiteSpace(workingRootPath)) {
            throw new ArgumentException("Working root path must be provided.", nameof(workingRootPath));
        }

        return Path.Combine(Path.GetFullPath(workingRootPath), PackageSourceDirectoryName);
    }

}
