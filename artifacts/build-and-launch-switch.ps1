[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Project,

    [Parameter(Mandatory = $true)]
    [string]$Output,

    [Parameter()]
    [string]$PackagePath = "",

    [Parameter()]
    [string]$BuildScriptPath = "C:\dev\helworks\helengine\artifacts\build-platform.ps1",

    [Parameter()]
    [string]$EmulatorPath = "C:\dev\helworks\emus\Eden-Windows-v0.2.0-rc2-amd64-msvc-standard\eden-cli.exe"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RequiredFilePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Label,

        [Parameter(Mandatory = $true)]
        [int]$ExitCode
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        [Console]::Error.WriteLine("$Label is required.")
        exit $ExitCode
    }

    $ResolvedPath = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $ResolvedPath -PathType Leaf)) {
        [Console]::Error.WriteLine("$Label was not found at '$ResolvedPath'.")
        exit $ExitCode
    }

    return $ResolvedPath
}

function Resolve-RequiredDirectoryPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Label,

        [Parameter(Mandatory = $true)]
        [int]$ExitCode
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        [Console]::Error.WriteLine("$Label is required.")
        exit $ExitCode
    }

    return [System.IO.Path]::GetFullPath($Path)
}

function Resolve-BuiltPackagePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$OutputRoot,

        [Parameter()]
        [string]$PreferredPackagePath
    )

    if (-not [string]::IsNullOrWhiteSpace($PreferredPackagePath)) {
        return Resolve-RequiredFilePath -Path $PreferredPackagePath -Label "PackagePath" -ExitCode 6
    }

    $ResolvedOutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
    if (-not (Test-Path -LiteralPath $ResolvedOutputRoot -PathType Container)) {
        [Console]::Error.WriteLine("Output directory was not found at '$ResolvedOutputRoot'.")
        exit 7
    }

    $Candidates = Get-ChildItem -LiteralPath $ResolvedOutputRoot -Filter *.nro -Recurse -File | Sort-Object LastWriteTimeUtc -Descending
    if ($Candidates.Count -eq 0) {
        [Console]::Error.WriteLine("No .nro package was found under '$ResolvedOutputRoot'.")
        exit 8
    }

    return $Candidates[0].FullName
}

$ResolvedBuildScriptPath = Resolve-RequiredFilePath -Path $BuildScriptPath -Label "BuildScriptPath" -ExitCode 2
$ResolvedProjectPath = Resolve-RequiredFilePath -Path $Project -Label "Project" -ExitCode 3
$ResolvedOutputPath = Resolve-RequiredDirectoryPath -Path $Output -Label "Output" -ExitCode 4
$ResolvedEmulatorPath = Resolve-RequiredFilePath -Path $EmulatorPath -Label "EmulatorPath" -ExitCode 5

Write-Host ("Project: " + $ResolvedProjectPath)
Write-Host ("Output: " + $ResolvedOutputPath)
Write-Host ("Build Script: " + $ResolvedBuildScriptPath)
Write-Host ("Emulator: " + $ResolvedEmulatorPath)

& $ResolvedBuildScriptPath -Project $ResolvedProjectPath -Platform "switch" -Output $ResolvedOutputPath
if ($LASTEXITCODE -ne 0) {
    [Console]::Error.WriteLine("Switch platform build failed with exit code $LASTEXITCODE.")
    exit $LASTEXITCODE
}

$ResolvedPackagePath = Resolve-BuiltPackagePath -OutputRoot $ResolvedOutputPath -PreferredPackagePath $PackagePath
$PackageFile = Get-Item -LiteralPath $ResolvedPackagePath
Write-Host ("Package: " + $PackageFile.FullName)
Write-Host ("Package LastWriteTime: " + $PackageFile.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"))
Write-Host ("Launching: " + $ResolvedEmulatorPath)

try {
    $EmulatorProcess = Start-Process -FilePath $ResolvedEmulatorPath -ArgumentList @($ResolvedPackagePath) -WorkingDirectory (Split-Path $ResolvedEmulatorPath) -PassThru
    Write-Host ("Emulator PID: " + $EmulatorProcess.Id)
} catch {
    [Console]::Error.WriteLine("Failed to launch the Switch emulator: " + $_.Exception.Message)
    exit 9
}

exit 0
