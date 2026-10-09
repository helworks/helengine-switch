using helengine.baseplatform.Manifest;

namespace helengine.switch_platform.builder;

/// <summary>
/// Copies cooked runtime payloads from the staged package root into the staged Switch RomFS tree.
/// </summary>
public sealed class SwitchRomFsAssetStager {
    /// <summary>
    /// Stages the cooked payload files referenced by the build manifest into RomFS.
    /// </summary>
    /// <param name="manifest">Build manifest that identifies the cooked payload files to stage.</param>
    /// <param name="sourceRootPath">Package root that already contains the cooked payload files.</param>
    /// <param name="romFsRootPath">RomFS root that should receive the cooked payload files.</param>
    public void Stage(PlatformBuildManifest manifest, string sourceRootPath, string romFsRootPath) {
        if (manifest == null) {
            throw new ArgumentNullException(nameof(manifest));
        } else if (string.IsNullOrWhiteSpace(sourceRootPath)) {
            throw new ArgumentException("RomFS source root path must be provided.", nameof(sourceRootPath));
        } else if (string.IsNullOrWhiteSpace(romFsRootPath)) {
            throw new ArgumentException("RomFS destination root path must be provided.", nameof(romFsRootPath));
        }

        string fullSourceRootPath = Path.GetFullPath(sourceRootPath);
        string fullRomFsRootPath = Path.GetFullPath(romFsRootPath);
        string sourceRootPrefix = EnsureTrailingDirectorySeparator(fullSourceRootPath);
        HashSet<string> stagedRelativePaths = new(StringComparer.OrdinalIgnoreCase);

        Directory.CreateDirectory(fullRomFsRootPath);

        for (int index = 0; index < manifest.Scenes.Length; index++) {
            PlatformBuildPayloadReference[] payloadReferences = manifest.Scenes[index].PayloadReferences;
            for (int payloadIndex = 0; payloadIndex < payloadReferences.Length; payloadIndex++) {
                StagePayloadReference(payloadReferences[payloadIndex], sourceRootPrefix, fullRomFsRootPath, stagedRelativePaths);
            }
        }

        for (int index = 0; index < manifest.LooseAssets.Length; index++) {
            StagePayloadReference(manifest.LooseAssets[index].PayloadReference, sourceRootPrefix, fullRomFsRootPath, stagedRelativePaths);
        }
        foreach (PlatformCookWorkItem item in manifest.PlatformCookWorkItems) {
            StagePayloadReference(new PlatformBuildPayloadReference(item.OutputLogicalArtifactId, item.OutputRelativePath), sourceRootPrefix, fullRomFsRootPath, stagedRelativePaths);
        }
        foreach (PlatformBuildArtifact artifact in manifest.CookedArtifacts) {
            StagePayloadReference(new PlatformBuildPayloadReference(artifact.LogicalArtifactId, artifact.RelativePath), sourceRootPrefix, fullRomFsRootPath, stagedRelativePaths);
        }
    }

    /// <summary>
    /// Stages one payload reference into RomFS when it has not already been copied.
    /// </summary>
    /// <param name="payloadReference">Payload reference that identifies one cooked file.</param>
    /// <param name="sourceRootPrefix">Normalized source-root path with a trailing directory separator.</param>
    /// <param name="romFsRootPath">RomFS root that receives the copied file.</param>
    /// <param name="stagedRelativePaths">Set of already staged relative paths.</param>
    static void StagePayloadReference(
        PlatformBuildPayloadReference payloadReference,
        string sourceRootPrefix,
        string romFsRootPath,
        HashSet<string> stagedRelativePaths) {
        if (payloadReference == null) {
            throw new ArgumentNullException(nameof(payloadReference));
        } else if (stagedRelativePaths == null) {
            throw new ArgumentNullException(nameof(stagedRelativePaths));
        }

        string relativePath = NormalizeRelativePath(payloadReference.SourceIdentity);
        if (!stagedRelativePaths.Add(relativePath)) {
            return;
        }

        string sourceRootPath = sourceRootPrefix.TrimEnd(Path.DirectorySeparatorChar);
        string sourceFilePath = Path.GetFullPath(Path.Combine(sourceRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!sourceFilePath.StartsWith(sourceRootPrefix, StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException($"Payload '{payloadReference.SourceIdentity}' resolved outside the staged package root.");
        } else if (!File.Exists(sourceFilePath)) {
            throw new FileNotFoundException($"Staged payload '{payloadReference.SourceIdentity}' was not found.", sourceFilePath);
        }

        string destinationFilePath = Path.Combine(romFsRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        string destinationDirectoryPath = Path.GetDirectoryName(destinationFilePath)
            ?? throw new InvalidOperationException($"Destination directory could not be resolved for '{destinationFilePath}'.");
        Directory.CreateDirectory(destinationDirectoryPath);
        File.Copy(sourceFilePath, destinationFilePath, true);
    }

    /// <summary>
    /// Normalizes one authored relative path into forward-slash form.
    /// </summary>
    /// <param name="relativePath">Authored relative path.</param>
    /// <returns>Normalized relative path.</returns>
    static string NormalizeRelativePath(string relativePath) {
        if (string.IsNullOrWhiteSpace(relativePath)) {
            throw new InvalidOperationException("Payload source identity must be provided.");
        }

        string normalizedRelativePath = relativePath.Replace('\\', '/').TrimStart('/');
        if (normalizedRelativePath.Contains("..", StringComparison.Ordinal)) {
            throw new InvalidOperationException($"Payload source identity '{relativePath}' must stay inside the staged package root.");
        }

        return normalizedRelativePath;
    }

    /// <summary>
    /// Ensures one absolute path ends with the current directory separator.
    /// </summary>
    /// <param name="path">Absolute path to normalize.</param>
    /// <returns>Absolute path with a trailing directory separator.</returns>
    static string EnsureTrailingDirectorySeparator(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            throw new ArgumentException("Path must be provided.", nameof(path));
        }

        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}
