#include "platform/switch/SwitchRuntimeTexture2D.hpp"

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include <memory>
#include <stdexcept>
#include "TextureAssetPixelCodec.hpp"
namespace helengine::switch_platform {
    /// Creates one empty Switch runtime texture.
    SwitchRuntimeTexture2D::SwitchRuntimeTexture2D()
        : Pixels(Array<uint8_t>::Empty()), NativeColors(Array<uint8_t>::Empty()) {
    }

    /// Validates before retaining independently owned native storage and logical decoded pixels.
    SwitchRuntimeTexture2D::SwitchRuntimeTexture2D(TextureAsset* asset) : SwitchRuntimeTexture2D() {
        if (asset == nullptr) throw std::invalid_argument("Switch source texture is required.");
        std::unique_ptr<Array<uint8_t>> decoded(TextureAssetPixelCodec::DecodeToRgba32(asset));
        std::unique_ptr<Array<uint8_t>> native;
        if (asset->ColorFormat == TextureAssetColorFormat::SwitchNative) {
            native.reset(new Array<uint8_t>(asset->Colors->Length));
            Array<uint8_t>::Copy(asset->Colors, native.get(), asset->Colors->Length);
        }
        set_Width(asset->Width); set_Height(asset->Height);
        Pixels = decoded.release();
        if (native != nullptr) NativeColors = native.release();
    }

    /// Releases owned source-independent storage at the normal renderer texture-release boundary.
    SwitchRuntimeTexture2D::~SwitchRuntimeTexture2D() {
        if (Pixels != nullptr && Pixels != Array<uint8_t>::Empty()) delete Pixels;
        if (NativeColors != nullptr && NativeColors != Array<uint8_t>::Empty()) delete NativeColors;
    }
}
#endif
