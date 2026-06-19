param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactPath,

    [Parameter()]
    [string]$EmulatorPath = "C:\dev\helworks\emus\Eden-Windows-v0.2.0-rc2-amd64-msvc-standard\eden-cli.exe"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ResolvedArtifactPath = [System.IO.Path]::GetFullPath($ArtifactPath)
if (-not (Test-Path -LiteralPath $ResolvedArtifactPath -PathType Leaf)) {
    [Console]::Error.WriteLine("Artifact file was not found at '$ResolvedArtifactPath'.")
    exit 3
}

if ([System.IO.Path]::GetExtension($ResolvedArtifactPath) -ine ".nro") {
    [Console]::Error.WriteLine("Expected a .nro artifact at '$ResolvedArtifactPath'.")
    exit 4
}

$ResolvedEmulatorPath = [System.IO.Path]::GetFullPath($EmulatorPath)
if (-not (Test-Path -LiteralPath $ResolvedEmulatorPath -PathType Leaf)) {
    [Console]::Error.WriteLine("Emulator executable was not found at '$ResolvedEmulatorPath'.")
    exit 5
}

$EmulatorProcess = Start-Process -FilePath $ResolvedEmulatorPath -ArgumentList @($ResolvedArtifactPath) -WorkingDirectory (Split-Path -Path $ResolvedEmulatorPath -Parent) -PassThru
Write-Host ("ARTIFACT: " + $ResolvedArtifactPath)
Write-Host ("PROCESS_ID: " + $EmulatorProcess.Id)
