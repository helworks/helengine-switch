using System.Reflection;

namespace helengine.switch_platform.builder.tests;

/// <summary>
/// Verifies repository-root resolution uses the Switch builder assembly location before the host application's base directory.
/// </summary>
public sealed class SwitchRepositoryRootResolverTests {
    /// <summary>
    /// Ensures copied editor outputs still resolve the Switch repository root from the original builder assembly location.
    /// </summary>
    [Fact]
    public void ResolveRepositoryRootPath_prefers_builder_assembly_location_when_app_base_directory_is_unrelated() {
        string rootPath = Path.Combine(Path.GetTempPath(), "helengine-switch-builder-tests", Guid.NewGuid().ToString("N"));
        try {
            string repositoryRootPath = Path.Combine(rootPath, "helengine-switch");
            string builderAssemblyPath = Path.Combine(repositoryRootPath, "builder", "bin", "Debug", "net9.0", "helengine.switch.builder.dll");
            string unrelatedAppBaseDirectory = Path.Combine(rootPath, "editor-output");

            Directory.CreateDirectory(Path.GetDirectoryName(builderAssemblyPath) ?? throw new InvalidOperationException("Builder assembly directory is required."));
            Directory.CreateDirectory(Path.Combine(repositoryRootPath, "src", "platform", "switch"));
            Directory.CreateDirectory(unrelatedAppBaseDirectory);
            File.WriteAllText(Path.Combine(repositoryRootPath, "Makefile"), "all:");
            File.WriteAllText(Path.Combine(repositoryRootPath, "src", "platform", "switch", "SwitchBootHost.cpp"), "// boot");
            File.WriteAllText(builderAssemblyPath, string.Empty);

            Type resolverType = typeof(SwitchPlatformAssetBuilder).Assembly.GetType(
                "helengine.switch_platform.builder.SwitchRepositoryRootResolver",
                throwOnError: false)
                ?? throw new InvalidOperationException("Switch repository root resolver type was not found.");
            MethodInfo resolveMethod = resolverType.GetMethod(
                "ResolveRepositoryRootPath",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                [typeof(string), typeof(string), typeof(string)],
                null)
                ?? throw new InvalidOperationException("Switch repository root resolver method was not found.");
            object resolvedRootPathObject = resolveMethod.Invoke(null, [string.Empty, builderAssemblyPath, unrelatedAppBaseDirectory])
                ?? throw new InvalidOperationException("Resolved repository root path was not returned.");

            Assert.Equal(Path.GetFullPath(repositoryRootPath), Assert.IsType<string>(resolvedRootPathObject));
        } finally {
            if (Directory.Exists(rootPath)) {
                Directory.Delete(rootPath, true);
            }
        }
    }

    /// <summary>
    /// Ensures copied editor outputs can resolve the Switch repository root from the Helengine platform installation manifest.
    /// </summary>
    [Fact]
    public void ResolveRepositoryRootPath_uses_helengine_platform_manifest_when_builder_assembly_path_is_copied() {
        string rootPath = Path.Combine(Path.GetTempPath(), "helengine-switch-builder-tests", Guid.NewGuid().ToString("N"));
        try {
            string helengineRootPath = Path.Combine(rootPath, "helengine");
            string repositoryRootPath = Path.Combine(rootPath, "helengine-switch");
            string copiedBuilderAssemblyPath = Path.Combine(helengineRootPath, "out", "editor-switch-run", "Debug", "net9.0", "helengine.switch.builder.dll");
            string appBaseDirectory = Path.Combine(helengineRootPath, "out", "editor-switch-run", "Debug", "net9.0");
            string userSettingsRootPath = Path.Combine(helengineRootPath, "user_settings");

            Directory.CreateDirectory(Path.Combine(helengineRootPath, "engine", "helengine.editor"));
            Directory.CreateDirectory(userSettingsRootPath);
            Directory.CreateDirectory(Path.GetDirectoryName(copiedBuilderAssemblyPath) ?? throw new InvalidOperationException("Copied builder assembly directory is required."));
            Directory.CreateDirectory(Path.Combine(repositoryRootPath, "src", "platform", "switch"));
            File.WriteAllText(Path.Combine(helengineRootPath, "engine", "helengine.editor", "helengine.editor.csproj"), "<Project />");
            File.WriteAllText(Path.Combine(repositoryRootPath, "Makefile"), "all:");
            File.WriteAllText(Path.Combine(repositoryRootPath, "src", "platform", "switch", "SwitchBootHost.cpp"), "// boot");
            File.WriteAllText(copiedBuilderAssemblyPath, string.Empty);
            File.WriteAllText(Path.Combine(userSettingsRootPath, "platforms.json"), $$"""
{
  "platforms": [
    {
      "engineVersion": "1.0.0",
      "platformId": "switch",
      "displayName": "Nintendo Switch",
      "builderAssemblyPath": "../../helengine-switch/builder/bin/Debug/net9.0/helengine.switch.builder.dll",
      "playerSourceRootPath": "../../helengine-switch",
      "pluginManifestPath": "../../helengine-switch/platform-plugin.json"
    }
  ]
}
""");

            Type resolverType = typeof(SwitchPlatformAssetBuilder).Assembly.GetType(
                "helengine.switch_platform.builder.SwitchRepositoryRootResolver",
                throwOnError: false)
                ?? throw new InvalidOperationException("Switch repository root resolver type was not found.");
            MethodInfo resolveMethod = resolverType.GetMethod(
                "ResolveRepositoryRootPath",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                [typeof(string), typeof(string), typeof(string)],
                null)
                ?? throw new InvalidOperationException("Switch repository root resolver method was not found.");
            object resolvedRootPathObject = resolveMethod.Invoke(null, [string.Empty, copiedBuilderAssemblyPath, appBaseDirectory])
                ?? throw new InvalidOperationException("Resolved repository root path was not returned.");

            Assert.Equal(Path.GetFullPath(repositoryRootPath), Assert.IsType<string>(resolvedRootPathObject));
        } finally {
            if (Directory.Exists(rootPath)) {
                Directory.Delete(rootPath, true);
            }
        }
    }
}
