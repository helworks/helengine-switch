#include "platform/switch/SwitchPackagedAssetLoader.hpp"

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include <cstdio>
#include <stdexcept>

#include "Asset.hpp"
#include "AssetSerializer.hpp"
#include "SceneAsset.hpp"
#include "runtime/runtime_startup_manifest.hpp"
#include "system/io/file.hpp"

namespace helengine::switch_platform {
    /// Normalizes one RomFS root path to forward slashes without a trailing separator.
    std::string SwitchPackagedAssetLoader::NormalizeRootPath(const std::string& value) {
        if (value.empty()) {
            throw std::invalid_argument("Switch content root path is required.");
        }

        std::string normalized = value;
        for (std::size_t index = 0; index < normalized.size(); index++) {
            if (normalized[index] == '\\') {
                normalized[index] = '/';
            }
        }

        while (normalized.size() > 1 && normalized.back() == '/') {
            normalized.pop_back();
        }

        return normalized;
    }

    /// Normalizes one cooked-relative path to forward slashes.
    std::string SwitchPackagedAssetLoader::NormalizeRelativePath(const std::string& value) {
        if (value.empty()) {
            throw std::invalid_argument("Switch cooked-relative asset path is required.");
        }

        std::string normalized = value;
        for (std::size_t index = 0; index < normalized.size(); index++) {
            if (normalized[index] == '\\') {
                normalized[index] = '/';
            }
        }

        if (normalized[0] == '/') {
            throw std::invalid_argument("Switch cooked-relative asset path must not be rooted.");
        } else if (normalized.find("..") != std::string::npos) {
            throw std::invalid_argument("Switch cooked-relative asset path must stay inside RomFS.");
        }

        return normalized;
    }

    /// Creates one packaged asset loader for the supplied RomFS content root.
    SwitchPackagedAssetLoader::SwitchPackagedAssetLoader(const std::string& contentRootPath)
        : ContentRootPath(NormalizeRootPath(contentRootPath)) {
    }

    /// Builds one full RomFS asset path from the normalized root and cooked-relative path.
    std::string SwitchPackagedAssetLoader::BuildContentPath(const std::string& cookedRelativePath) const {
        std::string normalizedRelativePath = NormalizeRelativePath(cookedRelativePath);
        return ContentRootPath + "/" + normalizedRelativePath;
    }

    /// Determines whether one packaged asset exists in RomFS.
    bool SwitchPackagedAssetLoader::AssetExists(const std::string& cookedRelativePath) const {
        std::string fullPath = BuildContentPath(cookedRelativePath);
        FILE* file = std::fopen(fullPath.c_str(), "rb");
        if (file == nullptr) {
            return false;
        }

        std::fclose(file);
        return true;
    }

    /// Loads one serialized packaged asset by cooked-relative path.
    Asset* SwitchPackagedAssetLoader::LoadAsset(const std::string& cookedRelativePath) const {
        std::string fullPath = BuildContentPath(cookedRelativePath);
        if (!File::Exists(fullPath)) {
            throw std::runtime_error("Packaged Switch asset was not found: " + fullPath);
        }

        FileStream* stream = File::OpenRead(fullPath);
        if (stream == nullptr) {
            throw std::runtime_error("Packaged Switch asset could not be opened: " + fullPath);
        }

        Asset* asset = AssetSerializer::Deserialize(stream);
        delete stream;
        return asset;
    }

    /// Loads the configured startup scene using the generated startup manifest.
    SceneAsset* SwitchPackagedAssetLoader::LoadStartupScene() const {
        const char* startupSceneRelativePath = he_get_runtime_startup_scene_relative_path();
        if (startupSceneRelativePath == nullptr || startupSceneRelativePath[0] == '\0') {
            throw std::runtime_error("Switch runtime startup manifest did not define a startup scene.");
        }

        return static_cast<SceneAsset*>(LoadAsset(startupSceneRelativePath));
    }
}
#endif
