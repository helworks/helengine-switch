#pragma once

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include <string>

#include "ModelAsset.hpp"
#include "PlatformMaterialAsset.hpp"
#include "RenderManager3D.hpp"

class ContentManager;
class IContentStreamSource;

namespace helengine::switch_platform {
    class SwitchRuntimeMaterial;

    /// Provides a placeholder Switch 3D runtime surface used to satisfy generated-core contracts.
    class SwitchRenderManager3D final : public ::RenderManager3D {
    public:
        /// Creates the Switch 3D render manager.
        SwitchRenderManager3D();

        /// Builds a placeholder runtime material from one cooked platform-owned material payload.
        ::RuntimeMaterial* BuildMaterialFromCooked(::PlatformMaterialAsset* materialAsset) override;

        /// Builds a placeholder runtime material from one cooked platform-owned material payload serialized through the runtime content stream source.
        ::RuntimeMaterial* BuildMaterialFromCooked(HeCppString cookedAssetPath, ::IContentStreamSource* contentStreamSource) override;

        /// Builds a placeholder runtime model from the authored model asset.
        ::RuntimeModel* BuildModelFromRaw(::ModelAsset* data) override;

        /// Builds a placeholder runtime model from one serialized model payload resolved through the runtime content stream source.
        ::RuntimeModel* BuildModelFromCooked(HeCppString cookedAssetPath, ::IContentStreamSource* contentStreamSource) override;

        /// Releases one Switch runtime model after the final scene reference is removed.
        void ReleaseModel(::RuntimeModel* model) override;

        /// Releases one Switch runtime material after the final scene reference is removed.
        void ReleaseMaterial(::RuntimeMaterial* material) override;

        /// Draws the current frame as a no-op placeholder.
        void Draw() override;
    };
}
#endif
