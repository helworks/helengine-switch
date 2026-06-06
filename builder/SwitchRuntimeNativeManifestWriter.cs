using helengine.baseplatform.Manifest;

namespace helengine.switch_platform.builder;

/// <summary>
/// Writes generated C++ runtime manifest source files consumed by the Switch native player.
/// </summary>
public sealed class SwitchRuntimeNativeManifestWriter {
    /// <summary>
    /// Editor-owned runtime manifest writer reused for the Switch generated-core contract.
    /// </summary>
    readonly helengine.editor.EditorRuntimeNativeManifestWriter Writer;

    /// <summary>
    /// Initializes one Switch runtime manifest writer.
    /// </summary>
    public SwitchRuntimeNativeManifestWriter() {
        Writer = new helengine.editor.EditorRuntimeNativeManifestWriter();
    }

    /// <summary>
    /// Writes the Switch runtime manifest sources into the generated-core runtime folder.
    /// </summary>
    /// <param name="generatedCoreRootPath">Absolute generated-core root path.</param>
    /// <param name="manifest">Resolved build manifest whose runtime metadata should be embedded.</param>
    public void Write(string generatedCoreRootPath, PlatformBuildManifest manifest) {
        Writer.Write(generatedCoreRootPath, manifest);
    }
}
