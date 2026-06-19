namespace helengine.switch_platform.builder.tests;

/// <summary>
/// Guards the canonical Switch emulator launcher contract.
/// </summary>
public sealed class SwitchEmulatorLauncherScriptTests {
    /// <summary>
    /// Ensures the canonical launcher requires one explicit artifact path and starts the configured Switch emulator with an .nro package.
    /// </summary>
    [Fact]
    public void Launcher_RequiresArtifactPath_AndLaunchesSwitchEmulator() {
        string repositoryRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string scriptPath = Path.Combine(repositoryRootPath, "scripts", "launch_in_emulator.ps1");

        Assert.True(File.Exists(scriptPath), "Expected scripts/launch_in_emulator.ps1 to exist.");

        string scriptSource = File.ReadAllText(scriptPath);

        Assert.Contains("[string]$ArtifactPath", scriptSource, StringComparison.Ordinal);
        Assert.Contains(".nro", scriptSource, StringComparison.Ordinal);
        Assert.Contains("eden-cli.exe", scriptSource, StringComparison.Ordinal);
        Assert.DoesNotContain("[string]$Project", scriptSource, StringComparison.Ordinal);
    }
}
