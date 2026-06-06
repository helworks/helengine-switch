#pragma once

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include "RuntimeTexture.hpp"

namespace helengine::switch_platform {
    /// Carries one placeholder Switch runtime texture for the framebuffer-backed runtime seam.
    class SwitchRuntimeTexture2D : public RuntimeTexture {
    public:
        /// Creates one empty Switch runtime texture.
        SwitchRuntimeTexture2D();
    };
}
#endif
