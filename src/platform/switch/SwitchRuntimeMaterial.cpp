#include "platform/switch/SwitchRuntimeMaterial.hpp"

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
namespace helengine::switch_platform {
    /// Creates one Switch runtime material with visible defaults.
    SwitchRuntimeMaterial::SwitchRuntimeMaterial()
        : BaseColor(1.0f, 1.0f, 1.0f)
        , UseVertexColor(false)
        , LightingEnabled(true) {
    }
}
#endif
