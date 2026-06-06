#pragma once

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include "RuntimeModel.hpp"

namespace helengine::switch_platform {
    /// Carries one placeholder Switch runtime model for the current framebuffer-backed runtime seam.
    class SwitchRuntimeModel : public RuntimeModel {
    public:
        /// Creates one empty Switch runtime model.
        SwitchRuntimeModel();
    };
}
#endif
