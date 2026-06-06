#include "platform/switch/SwitchRenderManager3D.hpp"

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include <stdexcept>

#include "Asset.hpp"
#include "AssetSerializer.hpp"
#include "platform/switch/SwitchRuntimeMaterial.hpp"
#include "platform/switch/SwitchRuntimeModel.hpp"
#include "system/io/file.hpp"

namespace helengine::switch_platform {
    namespace {
        /// Converts one channel byte into its normalized float form.
        float NormalizeChannel(byte channel) {
            return static_cast<float>(channel) / 255.0f;
        }
    }

    /// Creates the Switch 3D render manager.
    SwitchRenderManager3D::SwitchRenderManager3D() {
    }

    /// Builds a placeholder runtime material from one cooked platform-owned material payload.
    RuntimeMaterial* SwitchRenderManager3D::BuildMaterialFromCooked(PlatformMaterialAsset* materialAsset) {
        if (materialAsset == nullptr) {
            throw std::invalid_argument("Switch cooked material asset is required.");
        }

        SwitchRuntimeMaterial* runtimeMaterial = new SwitchRuntimeMaterial();
        runtimeMaterial->BaseColor = float3(
            NormalizeChannel(materialAsset->BaseColorR),
            NormalizeChannel(materialAsset->BaseColorG),
            NormalizeChannel(materialAsset->BaseColorB));
        runtimeMaterial->UseVertexColor = materialAsset->UseVertexColor;
        runtimeMaterial->LightingEnabled = materialAsset->Lit;
        return runtimeMaterial;
    }

    /// Builds a placeholder runtime material from one cooked platform-owned material payload serialized on disk.
    RuntimeMaterial* SwitchRenderManager3D::BuildMaterialFromCooked(std::string cookedAssetPath) {
        if (!File::Exists(cookedAssetPath)) {
            throw std::runtime_error("Switch cooked material asset was not found: " + cookedAssetPath);
        }

        FileStream* stream = File::OpenRead(cookedAssetPath);
        if (stream == nullptr) {
            throw std::runtime_error("Switch cooked material asset could not be opened: " + cookedAssetPath);
        }

        Asset* asset = AssetSerializer::Deserialize(stream);
        delete stream;
        RuntimeMaterial* runtimeMaterial = BuildMaterialFromCooked(static_cast<PlatformMaterialAsset*>(asset));
        delete asset;
        return runtimeMaterial;
    }

    /// Builds a placeholder runtime model from the authored model asset.
    RuntimeModel* SwitchRenderManager3D::BuildModelFromRaw(ModelAsset* data) {
        if (data == nullptr) {
            throw std::invalid_argument("Switch raw model asset is required.");
        }

        return new SwitchRuntimeModel();
    }

    /// Builds a placeholder runtime model from one serialized model payload on disk.
    RuntimeModel* SwitchRenderManager3D::BuildModelFromCooked(std::string cookedAssetPath) {
        if (!File::Exists(cookedAssetPath)) {
            throw std::runtime_error("Switch cooked model asset was not found: " + cookedAssetPath);
        }

        FileStream* stream = File::OpenRead(cookedAssetPath);
        if (stream == nullptr) {
            throw std::runtime_error("Switch cooked model asset could not be opened: " + cookedAssetPath);
        }

        Asset* asset = AssetSerializer::Deserialize(stream);
        delete stream;
        RuntimeModel* runtimeModel = BuildModelFromRaw(static_cast<ModelAsset*>(asset));
        delete asset;
        return runtimeModel;
    }

    /// Releases one Switch runtime model after the final scene reference is removed.
    void SwitchRenderManager3D::ReleaseModel(RuntimeModel* model) {
        if (model != nullptr) {
            delete model;
        }
    }

    /// Releases one Switch runtime material after the final scene reference is removed.
    void SwitchRenderManager3D::ReleaseMaterial(RuntimeMaterial* material) {
        if (material != nullptr) {
            delete material;
        }
    }

    /// Draws the current frame as a no-op placeholder.
    void SwitchRenderManager3D::Draw() {
    }
}
#endif
