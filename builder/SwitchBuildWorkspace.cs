namespace helengine.switch_platform.builder;

/// <summary>
/// Describes the prepared filesystem layout consumed by the native Switch build and exported package.
/// </summary>
public sealed class SwitchBuildWorkspace {
    /// <summary>
    /// Initializes one Switch build workspace.
    /// </summary>
    /// <param name="repositoryRootPath">Switch repository root used by the native build.</param>
    /// <param name="workingRootPath">Temporary working root that contains staged inputs.</param>
    /// <param name="outputRootPath">Requested builder output root.</param>
    /// <param name="generatedCoreRootPath">Generated-core C++ root provided by the editor build graph when authored runtime content is being embedded.</param>
    /// <param name="romFsRootPath">Staged RomFS root on the host filesystem.</param>
    /// <param name="containerRomFsRootPath">RomFS root path seen by the Docker container.</param>
    /// <param name="containerGeneratedCoreRootPath">Generated-core root path seen by the Docker container when authored runtime content is being embedded.</param>
    /// <param name="repositoryPackagePath">Path where the native build emits the repository-local NRO.</param>
    /// <param name="exportPackagePath">Final exported package path returned to the caller.</param>
    public SwitchBuildWorkspace(
        string repositoryRootPath,
        string workingRootPath,
        string outputRootPath,
        string generatedCoreRootPath,
        string romFsRootPath,
        string containerRomFsRootPath,
        string containerGeneratedCoreRootPath,
        string repositoryPackagePath,
        string exportPackagePath) {
        if (string.IsNullOrWhiteSpace(repositoryRootPath)) {
            throw new ArgumentException("Repository root path is required.", nameof(repositoryRootPath));
        } else if (string.IsNullOrWhiteSpace(workingRootPath)) {
            throw new ArgumentException("Working root path is required.", nameof(workingRootPath));
        } else if (string.IsNullOrWhiteSpace(outputRootPath)) {
            throw new ArgumentException("Output root path is required.", nameof(outputRootPath));
        } else if (string.IsNullOrWhiteSpace(romFsRootPath)) {
            throw new ArgumentException("RomFS root path is required.", nameof(romFsRootPath));
        } else if (string.IsNullOrWhiteSpace(containerRomFsRootPath)) {
            throw new ArgumentException("Container RomFS root path is required.", nameof(containerRomFsRootPath));
        } else if (string.IsNullOrWhiteSpace(repositoryPackagePath)) {
            throw new ArgumentException("Repository package path is required.", nameof(repositoryPackagePath));
        } else if (string.IsNullOrWhiteSpace(exportPackagePath)) {
            throw new ArgumentException("Export package path is required.", nameof(exportPackagePath));
        } else if (string.IsNullOrWhiteSpace(generatedCoreRootPath) != string.IsNullOrWhiteSpace(containerGeneratedCoreRootPath)) {
            throw new ArgumentException("Generated core host and container paths must both be provided or both be empty.");
        }

        RepositoryRootPath = Path.GetFullPath(repositoryRootPath);
        WorkingRootPath = Path.GetFullPath(workingRootPath);
        OutputRootPath = Path.GetFullPath(outputRootPath);
        GeneratedCoreRootPath = string.IsNullOrWhiteSpace(generatedCoreRootPath)
            ? string.Empty
            : Path.GetFullPath(generatedCoreRootPath);
        RomFsRootPath = Path.GetFullPath(romFsRootPath);
        ContainerRomFsRootPath = containerRomFsRootPath;
        ContainerGeneratedCoreRootPath = containerGeneratedCoreRootPath ?? string.Empty;
        RepositoryPackagePath = Path.GetFullPath(repositoryPackagePath);
        ExportPackagePath = Path.GetFullPath(exportPackagePath);
    }

    /// <summary>
    /// Gets the Switch repository root used by the native build.
    /// </summary>
    public string RepositoryRootPath { get; }

    /// <summary>
    /// Gets the temporary working root that contains staged inputs.
    /// </summary>
    public string WorkingRootPath { get; }

    /// <summary>
    /// Gets the requested builder output root.
    /// </summary>
    public string OutputRootPath { get; }

    /// <summary>
    /// Gets the generated-core C++ root provided by the editor build graph when authored runtime content is being embedded.
    /// </summary>
    public string GeneratedCoreRootPath { get; }

    /// <summary>
    /// Gets the staged RomFS root on the host filesystem.
    /// </summary>
    public string RomFsRootPath { get; }

    /// <summary>
    /// Gets the RomFS root path seen by the Docker container.
    /// </summary>
    public string ContainerRomFsRootPath { get; }

    /// <summary>
    /// Gets the generated-core root path seen by the Docker container when authored runtime content is being embedded.
    /// </summary>
    public string ContainerGeneratedCoreRootPath { get; }

    /// <summary>
    /// Gets whether the prepared workspace includes one generated-core payload mount.
    /// </summary>
    public bool HasGeneratedCore => !string.IsNullOrWhiteSpace(GeneratedCoreRootPath);

    /// <summary>
    /// Gets the path where the native build emits the repository-local NRO.
    /// </summary>
    public string RepositoryPackagePath { get; }

    /// <summary>
    /// Gets the final exported package path returned to the caller.
    /// </summary>
    public string ExportPackagePath { get; }
}
