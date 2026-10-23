#include "platform/switch/SwitchRenderManager2D.hpp"

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include <stdexcept>
#include <memory>
#include <algorithm>
#include <cmath>
#include <cstring>
#include "Batch2DWriter.hpp"
#include "Batch2DDrawableEmitter.hpp"
#include "Batch2DCameraProjection.hpp"
#include "Batch2DVariant.hpp"
#include "CameraViewportResolver.hpp"
#include "RealMath.hpp"
#include "Core.hpp"
#include "Component.hpp"
#include "Entity.hpp"
#include "ObjectManager.hpp"
#include "ICamera.hpp"
#include "IClipRegion2D.hpp"
#include "IRenderQueue2D.hpp"
#include "IDrawable2D.hpp"
#include "IRoundedRectDrawable2D.hpp"
#include "ISpriteDrawable2D.hpp"
#include "ITextDrawable2D.hpp"

#include "TextureAsset.hpp"
#include "AssetSerializer.hpp"
#include "IContentStreamSource.hpp"
#include "runtime/finally.hpp"
#include "runtime/native_disposable.hpp"
#include "platform/switch/SwitchRuntimeTexture2D.hpp"

namespace helengine::switch_platform {
    SwitchRenderManager2D::SwitchRenderManager2D() : Writer(new ::Batch2DWriter(&Sink, 1024)), Emitter(new ::Batch2DDrawableEmitter(Writer, this)) {}
    SwitchRenderManager2D::~SwitchRenderManager2D() { delete Emitter; Writer->Dispose(); delete Writer; }
    void SwitchRenderManager2D::BeginFrame(uint32_t* pixels,int width,int height,int stride) { Width=width; Height=height; Sink.Begin(pixels,width,height,stride); }
    void SwitchRenderManager2D::EndFrame() { Writer->Flush(); Sink.End(); }
    void SwitchRenderManager2D::Draw() {
        auto* cameras = get_OwnerCore()->get_ObjectManager()->get_Cameras();
        for (int32_t i=0;i<cameras->get_Count();++i) {
            auto* camera=cameras->get_Item(i); if(camera==nullptr || camera->get_RenderQueue2D()==nullptr) continue;
            auto viewport=::CameraViewportResolver::ResolveViewport(camera->get_Viewport(),Width,Height);
            ClipLeft=std::max(0,static_cast<int>(::RealMath::Ceiling(viewport.X-0.5f)));
            ClipTop=std::max(0,static_cast<int>(::RealMath::Ceiling(viewport.Y-0.5f)));
            ClipRight=std::min(Width,static_cast<int>(::RealMath::Ceiling(viewport.X+viewport.Z-0.5f)));
            ClipBottom=std::min(Height,static_cast<int>(::RealMath::Ceiling(viewport.Y+viewport.W-0.5f)));
            if(ClipRight<=ClipLeft || ClipBottom<=ClipTop) continue;
            Writer->ConfigureCamera(::Batch2DCameraProjection::Create(viewport),viewport);
            camera->get_RenderQueue2D()->VisitOrdered(this); Writer->Flush();
        }
    }
    void SwitchRenderManager2D::Visit(::IDrawable2D* drawable) {
        if(drawable==nullptr) throw std::invalid_argument("Switch render queue contains a null drawable.");
        if(drawable->get_Parent()==nullptr || !drawable->get_Parent()->get_IsHierarchyEnabled()) return;
        drawable->Draw();
    }
    ::Batch2DRun SwitchRenderManager2D::ResolveRun(::Entity* owner) {
        int left=ClipLeft,top=ClipTop,right=ClipRight,bottom=ClipBottom;
        for(auto* entity=owner;entity!=nullptr;entity=entity->get_Parent()) {
            auto* components=entity->get_Components();
            for(int32_t i=0;components!=nullptr && i<components->get_Count();++i) {
                auto* clip=dynamic_cast<::IClipRegion2D*>(components->get_Item(i)); if(clip==nullptr) continue;
                auto rect=clip->GetClipRect();
                left=std::max(left,static_cast<int>(::RealMath::Ceiling(rect.X-0.5f))); top=std::max(top,static_cast<int>(::RealMath::Ceiling(rect.Y-0.5f)));
                right=std::min(right,static_cast<int>(::RealMath::Ceiling(rect.X+rect.Z-0.5f))); bottom=std::min(bottom,static_cast<int>(::RealMath::Ceiling(rect.Y+rect.W-0.5f)));
            }
        }
        return ::Batch2DRun(::Batch2DVariant::RoundedShape,nullptr,left,top,std::max(0,right-left),std::max(0,bottom-top));
    }
    /// Builds an independently owned Switch texture with validated native storage and decoded pixels.
    ::RuntimeTexture* SwitchRenderManager2D::BuildTextureFromRaw(::TextureAsset* data) {
        if (data == nullptr) {
            throw std::invalid_argument("Switch raw texture asset is required.");
        }

        return new SwitchRuntimeTexture2D(data);
    }

    /// Builds independent runtime pixels while disposing the source stream and transient decoded asset on every exit path.
    ::RuntimeTexture* SwitchRenderManager2D::BuildTextureFromCooked(HeCppString cookedAssetPath, ::IContentStreamSource* contentStreamSource) {
        if (::String::IsNullOrWhiteSpace(cookedAssetPath)) {
            throw std::invalid_argument("Switch cooked texture path is required.");
        }
        if (contentStreamSource == nullptr) {
            throw std::invalid_argument("Switch cooked texture content source is required.");
        }

        std::unique_ptr<::Stream> stream(contentStreamSource->OpenRead(cookedAssetPath));
        if (stream == nullptr) {
            throw std::runtime_error("Switch cooked texture content source returned a null stream.");
        }
        auto streamCleanup = he_cpp_make_scope_exit([&]() { stream->Dispose(); });
        std::unique_ptr<::Asset> asset(AssetSerializer::Deserialize(stream.get()));
        auto assetCleanup = he_cpp_make_scope_exit([&]() {
            if (auto* disposable = dynamic_cast<::IDisposable*>(asset.get())) {
                disposable->Dispose();
            }
        });
        auto* textureAsset = dynamic_cast<::TextureAsset*>(asset.get());
        if (textureAsset == nullptr) {
            throw std::invalid_argument("Switch cooked texture payload did not deserialize as TextureAsset.");
        }
        return BuildTextureFromRaw(textureAsset);
    }

    /// Releases one Switch runtime texture previously created by this renderer.
    void SwitchRenderManager2D::ReleaseTexture(::RuntimeTexture* texture) {
        if (texture != nullptr) {
            texture->Dispose();
            delete texture;
        }
    }

    /// Copies the shared rounded shape snapshot into the ordered writer.
    void SwitchRenderManager2D::DrawRoundedRect(::IRoundedRectDrawable2D* shape) {
        if(shape!=nullptr) Emitter->EmitRoundedRect(shape,ResolveRun(shape->get_Parent()));
    }

    /// Copies the shared transformed sprite snapshot into the ordered writer.
    void SwitchRenderManager2D::DrawSprite(::ISpriteDrawable2D* sprite) {
        if(sprite!=nullptr) Emitter->EmitSprite(sprite,ResolveRun(sprite->get_Parent()));
    }

    /// Copies shared font layout and effect passes into the ordered writer.
    void SwitchRenderManager2D::DrawText(::ITextDrawable2D* text) {
        if(text!=nullptr) Emitter->EmitText(text,ResolveRun(text->get_Parent()));
    }
    void SwitchRenderManager2D::UpdateTextureRegionCore(::RuntimeTexture* texture,int32_t x,int32_t y,int32_t width,int32_t height,Array<uint8_t>* rgba8,int32_t sourceRowPitch) {
        auto* owned=dynamic_cast<SwitchRuntimeTexture2D*>(texture);
        if(owned==nullptr) throw std::invalid_argument("Switch texture update requires a renderer-owned texture.");
        for(int32_t row=0;row<height;++row) std::memcpy(owned->Pixels->Data+((y+row)*owned->get_Width()+x)*4,rgba8->Data+row*sourceRowPitch,width*4);
        // Native transport describes the original immutable asset. A dynamic update owns decoded pixels only.
        if(owned->NativeColors!=nullptr && owned->NativeColors!=Array<uint8_t>::Empty()) delete owned->NativeColors;
        owned->NativeColors=Array<uint8_t>::Empty();
    }
}
#endif
