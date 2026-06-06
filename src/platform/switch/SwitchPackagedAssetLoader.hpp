#pragma once

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include <string>

class Asset;
class SceneAsset;

namespace helengine::switch_platform {
    /// Loads cooked packaged assets from the staged Switch RomFS content root.
    class SwitchPackagedAssetLoader {
    public:
        /// Creates the packaged asset loader for the supplied RomFS content root.
        explicit SwitchPackagedAssetLoader(const std::string& contentRootPath);

        /// Determines whether one packaged asset exists in RomFS.
        bool AssetExists(const std::string& cookedRelativePath) const;

        /// Loads one serialized packaged asset by cooked-relative path.
        Asset* LoadAsset(const std::string& cookedRelativePath) const;

        /// Loads the configured startup scene using the generated startup manifest.
        SceneAsset* LoadStartupScene() const;

    private:
        /// Normalizes one RomFS root path to forward slashes without a trailing separator.
        static std::string NormalizeRootPath(const std::string& value);

        /// Normalizes one cooked-relative path to forward slashes.
        static std::string NormalizeRelativePath(const std::string& value);

        /// Builds one full RomFS asset path from the normalized root and cooked-relative path.
        std::string BuildContentPath(const std::string& cookedRelativePath) const;

        /// Absolute RomFS content root that contains the cooked asset tree.
        std::string ContentRootPath;
    };
}
#endif
