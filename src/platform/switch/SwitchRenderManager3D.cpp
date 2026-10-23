#include "platform/switch/SwitchRenderManager3D.hpp"

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include <stdexcept>

#include "Asset.hpp"
#include "AssetSerializer.hpp"
#include "IContentStreamSource.hpp"
#include "platform/switch/SwitchRuntimeMaterial.hpp"
#include "platform/switch/SwitchRuntimeModel.hpp"

namespace helengine::switch_platform {
    namespace {
        /// Converts one channel byte into its normalized float form.
        float NormalizeChannel(uint8_t channel) {
            return static_cast<float>(channel) / 255.0f;
        }
    }

    /// Creates the Switch 3D render manager.
    SwitchRenderManager3D::SwitchRenderManager3D() {
    }

    /// Builds a placeholder runtime material from one cooked platform-owned material payload.
    ::RuntimeMaterial* SwitchRenderManager3D::BuildMaterialFromCooked(::PlatformMaterialAsset* materialAsset) {
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

    /// Builds a placeholder runtime material from one cooked platform-owned material payload serialized through the runtime content stream source.
    ::RuntimeMaterial* SwitchRenderManager3D::BuildMaterialFromCooked(HeCppString cookedAssetPath, ::IContentStreamSource* contentStreamSource) {
        if (cookedAssetPath.empty()) {
            throw std::invalid_argument("Switch cooked material asset path is required.");
        }
        if (contentStreamSource == nullptr) {
            throw std::invalid_argument("Switch content stream source is required for cooked material loading.");
        }

        ::Stream* stream = contentStreamSource->OpenRead(cookedAssetPath);
        if (stream == nullptr) {
            throw std::runtime_error("Switch cooked material asset could not be opened: " + cookedAssetPath.raw());
        }

        try {
            ::Asset* asset = AssetSerializer::Deserialize(stream);
            stream->Dispose();
            ::RuntimeMaterial* runtimeMaterial = BuildMaterialFromCooked(static_cast<::PlatformMaterialAsset*>(asset));
            delete asset;
            return runtimeMaterial;
        } catch (...) {
            stream->Dispose();
            throw;
        }
    }

    /// Builds a placeholder runtime model from the authored model asset.
    ::RuntimeModel* SwitchRenderManager3D::BuildModelFromRaw(::ModelAsset* data) {
        if (data == nullptr) {
            throw std::invalid_argument("Switch raw model asset is required.");
        }

        return new SwitchRuntimeModel();
    }

    /// Builds a placeholder runtime model from one serialized model payload resolved through the runtime content stream source.
    ::RuntimeModel* SwitchRenderManager3D::BuildModelFromCooked(HeCppString cookedAssetPath, ::IContentStreamSource* contentStreamSource) {
        if (cookedAssetPath.empty()) {
            throw std::invalid_argument("Switch cooked model asset path is required.");
        }
        if (contentStreamSource == nullptr) {
            throw std::invalid_argument("Switch content stream source is required for cooked model loading.");
        }

        ::Stream* stream = contentStreamSource->OpenRead(cookedAssetPath);
        if (stream == nullptr) {
            throw std::runtime_error("Switch cooked model asset could not be opened: " + cookedAssetPath.raw());
        }

        try {
            ::Asset* asset = AssetSerializer::Deserialize(stream);
            stream->Dispose();
            ::RuntimeModel* runtimeModel = BuildModelFromRaw(static_cast<::ModelAsset*>(asset));
            delete asset;
            return runtimeModel;
        } catch (...) {
            stream->Dispose();
            throw;
        }
    }

    /// Releases one Switch runtime model after the final scene reference is removed.
    void SwitchRenderManager3D::ReleaseModel(::RuntimeModel* model) {
        if (model != nullptr) {
            delete model;
        }
    }

    /// Releases one Switch runtime material after the final scene reference is removed.
    void SwitchRenderManager3D::ReleaseMaterial(::RuntimeMaterial* material) {
        if (material != nullptr) {
            delete material;
        }
    }

    /// Draws the current frame as a no-op placeholder.
    void SwitchRenderManager3D::Draw() {
    }
}
#endif
