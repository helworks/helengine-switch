using System.Text.Json.Nodes;

namespace helengine.switch_platform.builder;

/// <summary>
/// Creates the metadata-only platform plugin manifest used by editor platform discovery.
/// </summary>
public static class SwitchPlatformPluginManifest {
    /// <summary>
    /// Creates the JSON payload for the Nintendo Switch platform plugin manifest.
    /// </summary>
    /// <returns>Platform plugin manifest JSON object.</returns>
    public static JsonObject Create() {
        return new JsonObject {
            ["platformId"] = "switch",
            ["displayName"] = "Nintendo Switch",
            ["builderAssemblyPath"] = "builder/helengine.switch.builder.dll",
            ["definitionFactoryType"] = "helengine.switch_platform.builder.SwitchPlatformDefinitionFactory"
        };
    }
}
