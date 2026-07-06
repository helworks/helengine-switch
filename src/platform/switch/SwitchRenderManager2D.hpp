#pragma once

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include "RenderManager2D.hpp"

class IDrawable2D;
class IRoundedRectDrawable2D;
class ISpriteDrawable2D;
class ITextDrawable2D;
class TextureAsset;

namespace helengine::switch_platform {
    /// Provides a placeholder Switch 2D runtime surface used to satisfy generated-core contracts.
    class SwitchRenderManager2D final : public ::RenderManager2D {
    public:
        /// Builds a placeholder Switch runtime texture from raw texture metadata.
        ::RuntimeTexture* BuildTextureFromRaw(::TextureAsset* data) override;

        /// Releases one Switch runtime texture previously created by this renderer.
        void ReleaseTexture(::RuntimeTexture* texture) override;

        /// Draws a rounded rectangle as a no-op placeholder.
        void DrawRoundedRect(::IRoundedRectDrawable2D* shape) override;

        /// Draws a sprite as a no-op placeholder.
        void DrawSprite(::ISpriteDrawable2D* sprite) override;

        /// Draws text as a no-op placeholder.
        void DrawText(::ITextDrawable2D* text) override;
    };
}
#endif
