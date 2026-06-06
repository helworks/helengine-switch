#pragma once

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include "ShaderRuntimeMaterial.hpp"
#include "float3.hpp"

namespace helengine::switch_platform {
    /// Carries one placeholder Switch runtime material for the current framebuffer-backed runtime seam.
    class SwitchRuntimeMaterial : public ShaderRuntimeMaterial {
    public:
        /// Creates one Switch runtime material with visible defaults.
        SwitchRuntimeMaterial();

        /// Normalized authored base RGB color.
        float3 BaseColor;

        /// True when the authored material requested vertex-color modulation.
        bool UseVertexColor;

        /// True when the authored material requested lighting.
        bool LightingEnabled;
    };
}
#endif
