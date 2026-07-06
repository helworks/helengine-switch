using System.Text.Json;

namespace helengine.switch_platform.builder;

/// <summary>
/// Resolves the Switch repository root from the current builder execution context.
/// </summary>
static class SwitchRepositoryRootResolver {
    /// <summary>
    /// Environment variable that can override the Switch repository root.
    /// </summary>
    const string RepositoryRootEnvironmentVariableName = "HELENGINE_SWITCH_REPOSITORY_ROOT";

    /// <summary>
    /// Relative marker path used to detect the Helengine source root that owns the current editor build.
    /// </summary>
    const string HelengineEditorProjectRelativePath = "engine/helengine.editor/helengine.editor.csproj";

    /// <summary>
    /// Relative path to the shared platform installation manifest.
    /// </summary>
    const string PlatformInstallationManifestRelativePath = "user_settings/platforms.json";

    /// <summary>
    /// Resolves the repository root that contains the Switch native build using the current process context.
    /// </summary>
    /// <returns>Absolute Switch repository root path.</returns>
    public static string ResolveRepositoryRootPath() {
        string environmentOverride = Environment.GetEnvironmentVariable(RepositoryRootEnvironmentVariableName) ?? string.Empty;
        string builderAssemblyPath = typeof(SwitchRepositoryRootResolver).Assembly.Location;
        return ResolveRepositoryRootPath(environmentOverride, builderAssemblyPath, AppContext.BaseDirectory);
    }

    /// <summary>
    /// Resolves the repository root that contains the Switch native build using explicit candidate locations.
    /// </summary>
    /// <param name="environmentOverride">Optional environment override for the repository root.</param>
    /// <param name="builderAssemblyPath">Path to the loaded Switch builder assembly.</param>
    /// <param name="appContextBaseDirectory">Base directory of the host application that loaded the builder.</param>
    /// <returns>Absolute Switch repository root path.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when no candidate path resolves to the Switch repository root.</exception>
    public static string ResolveRepositoryRootPath(
        string environmentOverride,
        string builderAssemblyPath,
        string appContextBaseDirectory) {
        string repositoryRootPath = ResolveCandidateRootPath(environmentOverride);
        if (!string.IsNullOrWhiteSpace(repositoryRootPath)) {
            return repositoryRootPath;
        }

        string builderAssemblyDirectoryPath = string.IsNullOrWhiteSpace(builderAssemblyPath)
            ? string.Empty
            : Path.GetDirectoryName(Path.GetFullPath(builderAssemblyPath)) ?? string.Empty;
        repositoryRootPath = ResolveCandidateRootPath(builderAssemblyDirectoryPath);
        if (!string.IsNullOrWhiteSpace(repositoryRootPath)) {
            return repositoryRootPath;
        }

        repositoryRootPath = ResolveCandidateRootPath(appContextBaseDirectory);
        if (!string.IsNullOrWhiteSpace(repositoryRootPath)) {
            return repositoryRootPath;
        }

        repositoryRootPath = ResolveRepositoryRootPathFromHelenginePlatformManifest(appContextBaseDirectory);
        if (!string.IsNullOrWhiteSpace(repositoryRootPath)) {
            return repositoryRootPath;
        }

        throw new DirectoryNotFoundException("Unable to resolve the Switch repository root.");
    }

    /// <summary>
    /// Walks upward from one candidate location until the Switch repository root is found.
    /// </summary>
    /// <param name="candidatePath">Candidate path that may point at or beneath the Switch repository root.</param>
    /// <returns>The resolved repository root path, or an empty string when the candidate does not lead to the repository.</returns>
    static string ResolveCandidateRootPath(string candidatePath) {
        if (string.IsNullOrWhiteSpace(candidatePath)) {
            return string.Empty;
        }

        string path = Path.GetFullPath(candidatePath);
        while (!string.IsNullOrWhiteSpace(path)) {
            if (IsRepositoryRootPath(path)) {
                return path;
            }

            DirectoryInfo parent = Directory.GetParent(path);
            path = parent?.FullName ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>
    /// Resolves the Switch repository root from the Helengine platform installation manifest reachable from the current app base directory.
    /// </summary>
    /// <param name="appContextBaseDirectory">Base directory of the host application that loaded the builder.</param>
    /// <returns>The resolved repository root path, or an empty string when the Helengine platform manifest cannot resolve the Switch repository.</returns>
    static string ResolveRepositoryRootPathFromHelenginePlatformManifest(string appContextBaseDirectory) {
        string helengineRootPath = ResolveHelengineRootPath(appContextBaseDirectory);
        if (string.IsNullOrWhiteSpace(helengineRootPath)) {
            return string.Empty;
        }

        string platformInstallationManifestPath = Path.Combine(helengineRootPath, PlatformInstallationManifestRelativePath);
        if (!File.Exists(platformInstallationManifestPath)) {
            return string.Empty;
        }

        string userSettingsRootPath = Path.GetDirectoryName(platformInstallationManifestPath)
            ?? throw new InvalidOperationException("Platform installation manifest directory could not be resolved.");
        using FileStream stream = File.OpenRead(platformInstallationManifestPath);
        using JsonDocument document = JsonDocument.Parse(stream);
        if (!document.RootElement.TryGetProperty("platforms", out JsonElement platformsElement)
            || platformsElement.ValueKind != JsonValueKind.Array) {
            return string.Empty;
        }

        foreach (JsonElement platformElement in platformsElement.EnumerateArray()) {
            string platformId = GetOptionalPropertyValue(platformElement, "platformId");
            if (!string.Equals(platformId, "switch", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            string repositoryRootPath = ResolveCandidateRootPath(ResolveManifestPath(userSettingsRootPath, GetOptionalPropertyValue(platformElement, "playerSourceRootPath")));
            if (!string.IsNullOrWhiteSpace(repositoryRootPath)) {
                return repositoryRootPath;
            }

            string pluginManifestPath = ResolveManifestPath(userSettingsRootPath, GetOptionalPropertyValue(platformElement, "pluginManifestPath"));
            string pluginRepositoryRootPath = ResolveCandidateRootPath(Path.GetDirectoryName(pluginManifestPath) ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(pluginRepositoryRootPath)) {
                return pluginRepositoryRootPath;
            }

            string builderAssemblyPath = ResolveManifestPath(userSettingsRootPath, GetOptionalPropertyValue(platformElement, "builderAssemblyPath"));
            string builderRepositoryRootPath = ResolveCandidateRootPath(Path.GetDirectoryName(builderAssemblyPath) ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(builderRepositoryRootPath)) {
                return builderRepositoryRootPath;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Resolves the Helengine source root reachable from one application base directory.
    /// </summary>
    /// <param name="appContextBaseDirectory">Base directory of the host application that loaded the builder.</param>
    /// <returns>The resolved Helengine source root path, or an empty string when the directory is not inside a Helengine source checkout.</returns>
    static string ResolveHelengineRootPath(string appContextBaseDirectory) {
        if (string.IsNullOrWhiteSpace(appContextBaseDirectory)) {
            return string.Empty;
        }

        string path = Path.GetFullPath(appContextBaseDirectory);
        while (!string.IsNullOrWhiteSpace(path)) {
            string markerPath = Path.Combine(path, HelengineEditorProjectRelativePath);
            if (File.Exists(markerPath)) {
                return path;
            }

            DirectoryInfo parent = Directory.GetParent(path);
            path = parent?.FullName ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>
    /// Resolves one path declared in the Helengine platform installation manifest.
    /// </summary>
    /// <param name="userSettingsRootPath">Directory that owns the platform installation manifest file.</param>
    /// <param name="manifestPath">Absolute or relative path declared in the platform installation manifest.</param>
    /// <returns>Absolute path resolved against the manifest directory, or an empty string when the manifest path is missing.</returns>
    static string ResolveManifestPath(string userSettingsRootPath, string manifestPath) {
        if (string.IsNullOrWhiteSpace(userSettingsRootPath) || string.IsNullOrWhiteSpace(manifestPath)) {
            return string.Empty;
        }

        return Path.IsPathRooted(manifestPath)
            ? Path.GetFullPath(manifestPath)
            : Path.GetFullPath(Path.Combine(userSettingsRootPath, manifestPath));
    }

    /// <summary>
    /// Returns one optional string property value from one JSON object.
    /// </summary>
    /// <param name="element">JSON object that may contain the property.</param>
    /// <param name="propertyName">Property name to read.</param>
    /// <returns>The property value when present; otherwise an empty string.</returns>
    static string GetOptionalPropertyValue(JsonElement element, string propertyName) {
        if (!element.TryGetProperty(propertyName, out JsonElement propertyElement)) {
            return string.Empty;
        }

        return propertyElement.GetString() ?? string.Empty;
    }

    /// <summary>
    /// Determines whether one path points at the Switch repository root.
    /// </summary>
    /// <param name="path">Candidate repository root path.</param>
    /// <returns>True when the path looks like the Switch repository root.</returns>
    static bool IsRepositoryRootPath(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return false;
        }

        string makefilePath = Path.Combine(path, "Makefile");
        string bootHostPath = Path.Combine(path, "src", "platform", "switch", "SwitchBootHost.cpp");
        return File.Exists(makefilePath) && File.Exists(bootHostPath);
    }
}
