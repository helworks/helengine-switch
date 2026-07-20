using System.Diagnostics;
using helengine.baseplatform.Builders;

namespace helengine.switch_platform.builder;

/// <summary>
/// Invokes the Docker-based native Switch build using the staged generated-core root and RomFS tree.
/// </summary>
public sealed class SwitchNativeBuildExecutor : ISwitchNativeBuildExecutor {
    /// <summary>
    /// Executes the native Switch build for one prepared workspace.
    /// </summary>
    /// <param name="workspace">Prepared Switch build workspace.</param>
    /// <param name="cancellationToken">Cancellation token that can stop the native build.</param>
    public void Build(SwitchBuildWorkspace workspace, CancellationToken cancellationToken) {
        if (workspace == null) {
            throw new ArgumentNullException(nameof(workspace));
        }

        ProcessStartInfo startInfo = CreateStartInfo(workspace);
        NativeProcessRunResult result = new NativeProcessRunner().Run(startInfo, cancellationToken);
        if (result.ExitCode != 0) {
            throw new InvalidOperationException(
                $"The Switch native build failed with exit code {result.ExitCode}."
                + Environment.NewLine
                + result.StandardOutput
                + Environment.NewLine
                + result.StandardError);
        }
    }

    /// <summary>
    /// Creates the Docker process start-info for one prepared workspace.
    /// </summary>
    /// <param name="workspace">Prepared Switch build workspace.</param>
    /// <returns>Docker process start-info for the native Switch build.</returns>
    public static ProcessStartInfo CreateStartInfo(SwitchBuildWorkspace workspace) {
        if (workspace == null) {
            throw new ArgumentNullException(nameof(workspace));
        }

        ProcessStartInfo startInfo = new() {
            FileName = "docker",
            UseShellExecute = false,
            WorkingDirectory = workspace.RepositoryRootPath
        };

        IReadOnlyList<string> arguments = CreateBuildArguments(workspace);
        for (int index = 0; index < arguments.Count; index++) {
            startInfo.ArgumentList.Add(arguments[index]);
        }

        return startInfo;
    }

    /// <summary>
    /// Creates the Docker command arguments for one prepared workspace.
    /// </summary>
    /// <param name="workspace">Prepared Switch build workspace.</param>
    /// <returns>Ordered Docker command arguments.</returns>
    public static IReadOnlyList<string> CreateBuildArguments(SwitchBuildWorkspace workspace) {
        if (workspace == null) {
            throw new ArgumentNullException(nameof(workspace));
        }

        List<string> arguments = [
            "run",
            "--rm",
            "-v",
            $"{workspace.RepositoryRootPath}:/workspace",
            "-v",
            $"{workspace.RomFsRootPath}:{workspace.ContainerRomFsRootPath}",
            "-w",
            "/workspace",
            "helengine-switch",
            "make",
            "clean",
            "all",
            $"HELENGINE_SWITCH_ROMFS_ROOT={workspace.ContainerRomFsRootPath}"
        ];
        if (workspace.HasGeneratedCore) {
            arguments.Insert(4, "-v");
            arguments.Insert(5, $"{workspace.GeneratedCoreRootPath}:{workspace.ContainerGeneratedCoreRootPath}");
            arguments.Add($"HELENGINE_CORE_CPP_ROOT={workspace.ContainerGeneratedCoreRootPath}");
        }

        return arguments;
    }
}
