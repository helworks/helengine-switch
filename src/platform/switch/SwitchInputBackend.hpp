#pragma once

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include "IInputBackend.hpp"

namespace helengine::switch_platform {
    /// Captures the minimal Switch controller frame required to initialize generated core.
    class SwitchInputBackend final : public ::IInputBackend {
    public:
        /// Creates the Switch input backend.
        SwitchInputBackend();

        /// Captures one input frame from the Switch runtime.
        ::InputFrameState CaptureFrame() override;
    };
}
#endif
