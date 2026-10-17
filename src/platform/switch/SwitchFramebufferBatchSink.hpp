#pragma once
#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include "Batch2DSink.hpp"
#include "float4.hpp"
#include "float4x4.hpp"
#include "platform/switch/SwitchFramebufferRaster.hpp"
namespace helengine::switch_platform {
/// Consumes shared engine batches synchronously in the Switch linear framebuffer backend.
class SwitchFramebufferBatchSink final : public ::Batch2DSink {
public:
    /// Borrows one active frame for ordered draw calls.
    void Begin(uint32_t* pixels, int width, int height, int stride) { Raster.Begin(pixels,width,height,stride); }
    /// Clears the framebuffer borrow after all batches have flushed.
    void End() { Raster.End(); }
    /// Reports actual blended samples in the last borrowed framebuffer.
    uint64_t GetWrittenPixels() const { return Raster.GetWrittenPixels(); }
    /// Stores the engine's projection and physical viewport for subsequent batches.
    void SetCamera(::float4x4 projection, ::float4 viewport) override;
    /// Immediately rasterizes copied quad values and borrows textures only during submission.
    void Submit(::Batch2DBuffer* buffer, ::Batch2DRun run) override;
private:
    /// Owns the allocation-free software rasterizer.
    SwitchFramebufferRaster Raster;
    /// Stores the shared row-vector world-to-clip projection.
    ::float4x4 Projection;
    /// Stores the physical framebuffer viewport ordered x, y, width, height.
    ::float4 Viewport;
};
}
#endif
