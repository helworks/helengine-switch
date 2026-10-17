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
            float clipX=p.X*Projection.M11+p.Y*Projection.M21+p.Z*Projection.M31+p.W*Projection.M41;
            float clipY=p.X*Projection.M12+p.Y*Projection.M22+p.Z*Projection.M32+p.W*Projection.M42;
            quad[j] = {Viewport.X+(clipX+1)*Viewport.Z*0.5f, Viewport.Y+(1-clipY)*Viewport.W*0.5f,
                v.TexLocal.X,v.TexLocal.Y,v.TexLocal.Z,v.TexLocal.W};
        }
        float corners[]={a.Corners.X,a.Corners.Y,a.Corners.Z,a.Corners.W};
        Raster.Draw(quad, texture ? texture->Pixels->Data : nullptr, texture ? texture->get_Width() : 0, texture ? texture->get_Height() : 0,
            {a.Color.X,a.Color.Y,a.Color.Z,a.Color.W},{a.BorderColor.X,a.BorderColor.Y,a.BorderColor.Z,a.BorderColor.W},
            a.Shape.X,a.Shape.Y,a.Shape.Z,a.Shape.W,corners,run.get_ScissorX(),run.get_ScissorY(),
            run.get_ScissorX()+run.get_ScissorWidth(),run.get_ScissorY()+run.get_ScissorHeight());
    }
}
}
#endif
