namespace helengine.switch_platform.builder;

/// <summary>
/// Defines the execution boundary for the Docker-backed native Switch package build.
/// </summary>
public interface ISwitchNativeBuildExecutor {
    /// <summary>
    /// Executes the native Switch build for one prepared workspace.
    /// </summary>
    /// <param name="workspace">Prepared Switch build workspace.</param>
    /// <param name="cancellationToken">Cancellation token that can stop the native build.</param>
    void Build(SwitchBuildWorkspace workspace, CancellationToken cancellationToken);
}
