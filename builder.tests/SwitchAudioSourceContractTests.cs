namespace helengine.switch_platform.builder.tests;

/// <summary>
/// Guards the native Switch audio wiring contract across the boot host and Makefile.
/// </summary>
public sealed class SwitchAudioSourceContractTests {
    /// <summary>
    /// Ensures the Switch boot host constructs the native audio backend, assigns it to generated core, and compiles the audio source directory.
    /// </summary>
    [Fact]
    public void Source_contract_wires_switch_audio_backend_into_boot_host_and_makefile() {
        string repositoryRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string bootHostHeaderPath = Path.Combine(repositoryRootPath, "src", "platform", "switch", "SwitchBootHost.hpp");
        string bootHostSourcePath = Path.Combine(repositoryRootPath, "src", "platform", "switch", "SwitchBootHost.cpp");
        string backendHeaderPath = Path.Combine(repositoryRootPath, "src", "platform", "switch", "audio", "SwitchAudioBackend.hpp");
        string backendSourcePath = Path.Combine(repositoryRootPath, "src", "platform", "switch", "audio", "SwitchAudioBackend.cpp");
        string makefilePath = Path.Combine(repositoryRootPath, "Makefile");

        Assert.True(File.Exists(bootHostHeaderPath), "Expected SwitchBootHost.hpp to exist.");
        Assert.True(File.Exists(bootHostSourcePath), "Expected SwitchBootHost.cpp to exist.");
        Assert.True(File.Exists(backendHeaderPath), "Expected SwitchAudioBackend.hpp to exist.");
        Assert.True(File.Exists(backendSourcePath), "Expected SwitchAudioBackend.cpp to exist.");
        Assert.True(File.Exists(makefilePath), "Expected Makefile to exist.");

        string bootHostHeaderSource = File.ReadAllText(bootHostHeaderPath);
        string bootHostSource = File.ReadAllText(bootHostSourcePath);
        string makefileSource = File.ReadAllText(makefilePath);

        Assert.Contains("class SwitchAudioBackend;", bootHostHeaderSource, StringComparison.Ordinal);
        Assert.Contains("SwitchAudioBackend* EngineAudioBackend;", bootHostHeaderSource, StringComparison.Ordinal);
        Assert.Contains("platform/switch/audio/SwitchAudioBackend.hpp", bootHostSource, StringComparison.Ordinal);
        Assert.Contains("EngineAudioBackend = new SwitchAudioBackend();", bootHostSource, StringComparison.Ordinal);
        Assert.Contains("EngineCore->SetAudioBackend(EngineAudioBackend);", bootHostSource, StringComparison.Ordinal);
        Assert.Contains("delete EngineAudioBackend;", bootHostSource, StringComparison.Ordinal);
        Assert.Contains("src/platform/switch/audio", makefileSource, StringComparison.Ordinal);
    }
}
