using helengine.switch_platform.builder;

namespace helengine.switch_platform.builder.tests;

/// <summary>
/// Verifies the Switch native build executor wires the expected Docker arguments.
/// </summary>
public sealed class SwitchNativeBuildExecutorTests {
    /// <summary>
    /// Ensures the Docker command mounts the repository, generated core, and RomFS staging roots before invoking make.
    /// </summary>
    [Fact]
    public void CreateBuildArguments_includes_repository_generated_core_and_romfs_mounts() {
        SwitchBuildWorkspace workspace = new(
            @"C:\repo\helengine-switch",
            @"C:\repo\helengine-switch\tmp\working",
            @"C:\repo\helengine-switch\out",
            @"C:\repo\generated-core",
            @"C:\repo\helengine-switch\tmp\working\romfs",
            "/workspace-staging/romfs",
            "/generated-core",
            @"C:\repo\helengine-switch\build\helengine_switch.nro",
            @"C:\repo\helengine-switch\out\helengine_switch.nro");

        IReadOnlyList<string> arguments = SwitchNativeBuildExecutor.CreateBuildArguments(workspace);

        Assert.Contains("helengine-switch", arguments);
        Assert.Contains("make", arguments);
        Assert.DoesNotContain("clean", arguments);
        Assert.Contains(arguments, argument => argument.Contains(":/workspace/build", StringComparison.Ordinal));
        Assert.Contains("all", arguments);
        Assert.Contains("HELENGINE_SWITCH_ROMFS_ROOT=/workspace-staging/romfs", arguments);
        Assert.Contains("HELENGINE_CORE_CPP_ROOT=/generated-core", arguments);
    }

    /// <summary>
    /// Ensures the Docker command omits the generated-core mount and make variable when the builder is exporting one bootstrap package.
    /// </summary>
    [Fact]
    public void CreateBuildArguments_without_generated_core_omits_generated_core_mount() {
        SwitchBuildWorkspace workspace = new(
            @"C:\repo\helengine-switch",
            @"C:\repo\helengine-switch\tmp\working",
            @"C:\repo\helengine-switch\out",
            string.Empty,
            @"C:\repo\helengine-switch\tmp\working\romfs",
            "/workspace-staging/romfs",
            string.Empty,
            @"C:\repo\helengine-switch\build\helengine_switch.nro",
            @"C:\repo\helengine-switch\out\helengine_switch.nro");

        IReadOnlyList<string> arguments = SwitchNativeBuildExecutor.CreateBuildArguments(workspace);

        Assert.DoesNotContain($"{workspace.GeneratedCoreRootPath}:{workspace.ContainerGeneratedCoreRootPath}", arguments);
        Assert.DoesNotContain("HELENGINE_CORE_CPP_ROOT=/generated-core", arguments);
        Assert.Contains("HELENGINE_SWITCH_ROMFS_ROOT=/workspace-staging/romfs", arguments);
    }
}
