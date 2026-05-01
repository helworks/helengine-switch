#pragma once

#include <switch.h>

namespace helengine::switch_platform {
    /// Owns the first Switch native framebuffer bootstrap and yellow-frame presentation loop.
    class SwitchBootHost {
    public:
        /// Initializes the Switch boot path and keeps the first frame visible until shutdown.
        static int Run();

    private:
        /// Initializes a linear framebuffer bound to the default Switch window.
        static bool InitializeFramebuffer(Framebuffer& framebuffer);

        /// Renders one full yellow frame into the active framebuffer.
        static void RenderYellowFrame(Framebuffer& framebuffer);

        /// Builds the packed RGBA color value used for the first visible yellow frame.
        static u32 BuildYellowColor();
    };
}
