using helengine.baseplatform.Manifest;

namespace helengine.switch_platform.builder;

/// <summary>Executes Switch-owned texture/font work items inside a bounded package-source directory.</summary>
public sealed class SwitchTextureWorkItemCooker {
    /// <summary>Cooks and serializes one declared native or legacy texture output before RomFS staging.</summary>
    /// <param name="item">Declared texture or atlas work item to execute.</param>
    /// <param name="packageRoot">Bounded staging directory that receives the output.</param>
    /// <param name="numericPolicy">Target package policy; existing callers default to the current runtime policy.</param>
    public void Cook(PlatformCookWorkItem item, string packageRoot, NumericPrecisionPolicy numericPolicy = null) {
        if (item == null) throw new ArgumentNullException(nameof(item));
        numericPolicy ??= RealRepresentation.Policy;
        RealBinaryCodec.ValidatePolicy(numericPolicy);
        if (item.TargetPlatformId != "switch" || item.TargetArtifactKind != "runtime-texture"
            || (item.SourceAssetKind != "texture" && item.SourceAssetKind != "font-atlas-texture")) throw new InvalidOperationException("Unsupported Switch texture work item.");
        string root = Path.GetFullPath(packageRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string relative = item.OutputRelativePath.Replace('/', Path.DirectorySeparatorChar);
        string destination = Path.GetFullPath(Path.Combine(root, relative));
        if (Path.IsPathRooted(relative) || !destination.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Switch cooked output must remain within package-source.");
        bool fontAtlas = item.SourceAssetKind == "font-atlas-texture";
        string source = item.SourceAssetPath;
        // Editor-owned font externalization supplies the authoritative serialized atlas;
        // the accompanying packaged font deliberately has no embedded source texture.
        string sourceExtension = Path.GetExtension(source);
        bool serializedAtlas = string.Equals(sourceExtension, ".hetex", StringComparison.OrdinalIgnoreCase)
            || string.Equals(sourceExtension, ".hasset", StringComparison.OrdinalIgnoreCase);
        if (fontAtlas && !serializedAtlas) {
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
        File.WriteAllBytes(destination, helengine.files.AssetSerializer.SerializeToBytes(cooked, numericPolicy));
    }
}
