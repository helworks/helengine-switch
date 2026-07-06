using System.Text.Json.Nodes;

namespace helengine.switch_platform.builder.tests;

/// <summary>
/// Verifies the Switch external platform plugin manifest remains metadata-only and matches the checked-in file.
/// </summary>
public sealed class SwitchPlatformPluginManifestTests {
    /// <summary>
    /// Ensures the in-memory plugin manifest payload does not declare runtime payload extension metadata.
    /// </summary>
    [Fact]
    public void Create_whenSerialized_doesNotContainRuntimePayloadTypeDeclarations() {
        JsonObject manifest = SwitchPlatformPluginManifest.Create();

        Assert.Null(manifest["runtimePayloadTypes"]);
        Assert.Null(manifest["serializerHooks"]);
        Assert.Equal("switch", manifest["platformId"]?.GetValue<string>());
        Assert.Equal("Nintendo Switch", manifest["displayName"]?.GetValue<string>());
        Assert.Equal("builder/helengine.switch.builder.dll", manifest["builderAssemblyPath"]?.GetValue<string>());
    }

    /// <summary>
    /// Ensures the checked-in plugin manifest file matches the generated metadata-only payload.
    /// </summary>
    [Fact]
    public void CheckedInPluginManifest_matchesGeneratedMetadataOnlyPayload() {
        string manifestFilePath = Path.Combine(AppContext.BaseDirectory, "platform-plugin.json");
        string expectedJson = SwitchPlatformPluginManifest.Create().ToJsonString();
        string actualJson = JsonNode.Parse(File.ReadAllText(manifestFilePath))?.ToJsonString();

        Assert.Equal(expectedJson, actualJson);
    }
}
