using helengine.baseplatform.Manifest;

namespace helengine.switch_platform.builder;

/// <summary>Executes Switch-owned texture/font work items inside a bounded package-source directory.</summary>
public sealed class SwitchTextureWorkItemCooker {
    /// <summary>Cooks and serializes one declared native or legacy texture output before RomFS staging.</summary>
    public void Cook(PlatformCookWorkItem item, string packageRoot) {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (item.TargetPlatformId != "switch" || item.TargetArtifactKind != "runtime-texture"
            || (item.SourceAssetKind != "texture" && item.SourceAssetKind != "font-atlas-texture")) throw new InvalidOperationException("Unsupported Switch texture work item.");
        string root = Path.GetFullPath(packageRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string relative = item.OutputRelativePath.Replace('/', Path.DirectorySeparatorChar);
        string destination = Path.GetFullPath(Path.Combine(root, relative));
        if (Path.IsPathRooted(relative) || !destination.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Switch cooked output must remain within package-source.");
        bool fontAtlas = item.SourceAssetKind == "font-atlas-texture";
        string source = item.SourceAssetPath;
        if (fontAtlas) {
            string packagedFont = Path.ChangeExtension(destination, ".hefont");
            if (File.Exists(packagedFont)) source = packagedFont;
        }
        string assetId = null;
        foreach (PlatformCookWorkItemMetadata metadata in item.Metadata) if (metadata.Key == "source-asset-id") assetId = metadata.Value;
        if (string.IsNullOrWhiteSpace(assetId)) assetId = item.OutputLogicalArtifactId;
        if (fontAtlas) assetId += "#atlas";
        var settings = SwitchTextureCookSettingsSerializer.Deserialize(item.SerializedPlatformSettings);
        using TextureAsset cooked = new SwitchTextureSourceProcessor().Cook(source, assetId, settings, fontAtlas);
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        File.WriteAllBytes(destination, helengine.files.AssetSerializer.SerializeToBytes(cooked));
    }
}
