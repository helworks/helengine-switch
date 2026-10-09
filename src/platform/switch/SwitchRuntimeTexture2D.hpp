#pragma once

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include "RuntimeTexture.hpp"
#include "TextureAsset.hpp"
#include "runtime/array.hpp"

namespace helengine::switch_platform {
    /// Owns validated pixels and optional native Tegra storage for the framebuffer-backed runtime seam.
    class SwitchRuntimeTexture2D : public RuntimeTexture {
    public:
        /// Creates one empty Switch runtime texture.
        SwitchRuntimeTexture2D();

        /// Copies a transient source asset, validating native descriptors before retaining independent runtime storage.
        explicit SwitchRuntimeTexture2D(TextureAsset* asset);

        /// Releases runtime-owned pixels and native storage without deleting shared empty arrays.
        ~SwitchRuntimeTexture2D() override;

        /// Owns decoded logical RGBA pixels for portable CPU consumers.
        Array<uint8_t>* Pixels;

        /// Owns the preserved SWT1 descriptor and native GPU bytes for Switch-native sources.
        Array<uint8_t>* NativeColors;
    };
}
#endif
