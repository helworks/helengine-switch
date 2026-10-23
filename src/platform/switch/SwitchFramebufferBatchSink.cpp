#include "platform/switch/SwitchFramebufferBatchSink.hpp"
#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include "Batch2DBuffer.hpp"
#include "Batch2DRun.hpp"
#include "Batch2DVariant.hpp"
#include "Batch2DVertex.hpp"
#include "platform/switch/SwitchRuntimeTexture2D.hpp"
#include <stdexcept>
namespace helengine::switch_platform {
void SwitchFramebufferBatchSink::SetCamera(::float4x4 projection, ::float4 viewport) { Projection=projection; Viewport=viewport; }
void SwitchFramebufferBatchSink::Submit(::Batch2DBuffer* buffer, ::Batch2DRun run) {
    if (buffer == nullptr || buffer->VertexCount % 4 != 0) throw std::invalid_argument("Switch batch must contain complete quads.");
    auto* texture = dynamic_cast<SwitchRuntimeTexture2D*>(run.get_Texture());
    if (run.get_Variant() == ::Batch2DVariant::Textured && texture == nullptr) throw std::invalid_argument("Switch textured batch requires its renderer-owned texture.");
    for (int i=0;i<buffer->VertexCount;i+=4) {
        SwitchFramebufferRaster::Vertex quad[4];
        const auto& a = (*buffer->Vertices)[i];
        for(int j=0;j<4;++j) {
            const auto& v=(*buffer->Vertices)[i+j]; const auto& p=v.Position;
            const auto clipX=p.X*Projection.M11+p.Y*Projection.M21+p.Z*Projection.M31+p.W*Projection.M41;
            const auto clipY=p.X*Projection.M12+p.Y*Projection.M22+p.Z*Projection.M32+p.W*Projection.M42;
            // Keep configured projection arithmetic until the existing float raster transport boundary.
            quad[j] = {static_cast<float>(Viewport.X+(clipX+1)*Viewport.Z*0.5f), static_cast<float>(Viewport.Y+(1-clipY)*Viewport.W*0.5f),
                static_cast<float>(v.TexLocal.X),static_cast<float>(v.TexLocal.Y),static_cast<float>(v.TexLocal.Z),static_cast<float>(v.TexLocal.W)};
        }
        float corners[]={static_cast<float>(a.Corners.X),static_cast<float>(a.Corners.Y),static_cast<float>(a.Corners.Z),static_cast<float>(a.Corners.W)};
        Raster.Draw(quad, texture ? texture->Pixels->Data : nullptr, texture ? texture->get_Width() : 0, texture ? texture->get_Height() : 0,
            {static_cast<float>(a.Color.X),static_cast<float>(a.Color.Y),static_cast<float>(a.Color.Z),static_cast<float>(a.Color.W)},
            {static_cast<float>(a.BorderColor.X),static_cast<float>(a.BorderColor.Y),static_cast<float>(a.BorderColor.Z),static_cast<float>(a.BorderColor.W)},
            static_cast<float>(a.Shape.X),static_cast<float>(a.Shape.Y),static_cast<float>(a.Shape.Z),static_cast<float>(a.Shape.W),corners,run.get_ScissorX(),run.get_ScissorY(),
            run.get_ScissorX()+run.get_ScissorWidth(),run.get_ScissorY()+run.get_ScissorHeight());
    }
}
}
#endif
