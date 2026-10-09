#include "platform/switch/SwitchRenderManager2D.hpp"

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include <stdexcept>

#include "TextureAsset.hpp"
#include "platform/switch/SwitchRuntimeTexture2D.hpp"

namespace helengine::switch_platform {
    /// Builds an independently owned Switch texture with validated native storage and decoded pixels.
    ::RuntimeTexture* SwitchRenderManager2D::BuildTextureFromRaw(::TextureAsset* data) {
        if (data == nullptr) {
            throw std::invalid_argument("Switch raw texture asset is required.");
        }

        return new SwitchRuntimeTexture2D(data);
    }

    /// Releases one Switch runtime texture previously created by this renderer.
    void SwitchRenderManager2D::ReleaseTexture(::RuntimeTexture* texture) {
        if (texture != nullptr) {
            delete texture;
        }
    }

    /// Draws a rounded rectangle as a no-op placeholder.
    void SwitchRenderManager2D::DrawRoundedRect(::IRoundedRectDrawable2D*) {
    }

    /// Draws a sprite as a no-op placeholder.
    void SwitchRenderManager2D::DrawSprite(::ISpriteDrawable2D*) {
    }

    /// Draws text as a no-op placeholder.
    void SwitchRenderManager2D::DrawText(::ITextDrawable2D*) {
    }
}
#endif
