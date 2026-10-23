#pragma once

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include "RenderManager2D.hpp"
#include "IRenderVisitor2D.hpp"
#include "Batch2DRun.hpp"
#include "platform/switch/SwitchFramebufferBatchSink.hpp"

class IDrawable2D;
class IRoundedRectDrawable2D;
class ISpriteDrawable2D;
class ITextDrawable2D;
class TextureAsset;
class Batch2DWriter;
class Batch2DDrawableEmitter;
class Entity;

namespace helengine::switch_platform {
    /// Renders ordered authored UI geometry into the existing native Switch framebuffer surface.
    class SwitchRenderManager2D final : public ::RenderManager2D, public ::IRenderVisitor2D {
    public:
        /// Allocates reusable shared engine batch storage.
        SwitchRenderManager2D();
        /// Releases reusable batches without retaining framebuffer or drawable borrows.
        ~SwitchRenderManager2D() override;
        /// Borrows the current linear framebuffer until presentation.
        void BeginFrame(uint32_t* pixels, int width, int height, int stride);
        /// Flushes pending geometry and clears the framebuffer borrow.
        void EndFrame();
        /// Reports actual rasterized samples without exposing any framebuffer pixels.
        uint64_t GetWrittenPixels() const { return Sink.GetWrittenPixels(); }
        /// Visits camera queues in their engine order and emits shared drawable snapshots.
        void Draw() override;
        /// Skips disabled ancestor hierarchies and routes each ordered drawable to its owner.
        void Visit(::IDrawable2D* drawable) override;
        /// Builds an independently owned Switch runtime texture from raw texture metadata.
        ::RuntimeTexture* BuildTextureFromRaw(::TextureAsset* data) override;

        /// Reads one cooked texture through the active content source and releases transient asset storage after construction.
        ::RuntimeTexture* BuildTextureFromCooked(HeCppString cookedAssetPath, ::IContentStreamSource* contentStreamSource) override;

        /// Releases one Switch runtime texture previously created by this renderer.
        void ReleaseTexture(::RuntimeTexture* texture) override;

        /// Emits the engine's rounded shape geometry, border and corner mask.
        void DrawRoundedRect(::IRoundedRectDrawable2D* shape) override;

        /// Emits a tinted textured quad with the authored world transform.
        void DrawSprite(::ISpriteDrawable2D* sprite) override;

        /// Emits engine text layout and its ordered outline/shadow/coverage passes.
        void DrawText(::ITextDrawable2D* text) override;
    protected:
        /// Copies validated RGBA updates into the renderer-owned source-independent pixel storage.
        void UpdateTextureRegionCore(::RuntimeTexture* texture, int32_t x, int32_t y, int32_t width, int32_t height, Array<uint8_t>* rgba8, int32_t sourceRowPitch) override;
    private:
        /// Resolves the live intersection of camera and ancestor clip rectangles for one drawable.
        ::Batch2DRun ResolveRun(::Entity* owner);
        /// Owns the synchronous framebuffer batch consumer.
        SwitchFramebufferBatchSink Sink;
        /// Owns fixed shared vertex storage and borrows Sink.
        ::Batch2DWriter* Writer;
        /// Owns the shared drawable emitter, borrowing this renderer and Writer.
        ::Batch2DDrawableEmitter* Emitter;
        /// Stores current framebuffer dimensions and resolved camera clip bounds.
        int Width = 0, Height = 0, ClipLeft = 0, ClipTop = 0, ClipRight = 0, ClipBottom = 0;
    };
}
#endif
