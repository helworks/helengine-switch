#include "platform/switch/SwitchBootHost.hpp"

namespace helengine::switch_platform {
    namespace {
        constexpr u32 FramebufferWidth = 1280;
        constexpr u32 FramebufferHeight = 720;
    }

    /// Initializes the Switch boot path and keeps the first frame visible until shutdown.
    int SwitchBootHost::Run() {
        Framebuffer framebuffer;
        if (!InitializeFramebuffer(framebuffer)) {
            return 1;
        }

        while (appletMainLoop()) {
            RenderYellowFrame(framebuffer);
        }

        framebufferClose(&framebuffer);
        return 0;
    }

    /// Initializes a linear framebuffer bound to the default Switch window.
    bool SwitchBootHost::InitializeFramebuffer(Framebuffer& framebuffer) {
        NWindow* window = nwindowGetDefault();
        if (R_FAILED(framebufferCreate(&framebuffer, window, FramebufferWidth, FramebufferHeight, PIXEL_FORMAT_RGBA_8888, 2))) {
            return false;
        }

        if (R_FAILED(framebufferMakeLinear(&framebuffer))) {
            framebufferClose(&framebuffer);
            return false;
        }

        return true;
    }

    /// Renders one full yellow frame into the active framebuffer.
    void SwitchBootHost::RenderYellowFrame(Framebuffer& framebuffer) {
        u32 stride = 0;
        u32* pixels = static_cast<u32*>(framebufferBegin(&framebuffer, &stride));
        const u32 yellow = BuildYellowColor();
        const u32 stridePixels = stride / sizeof(u32);

        for (u32 y = 0; y < FramebufferHeight; ++y) {
            for (u32 x = 0; x < FramebufferWidth; ++x) {
                pixels[y * stridePixels + x] = yellow;
            }
        }

        framebufferEnd(&framebuffer);
    }

    /// Builds the packed RGBA color value used for the first visible yellow frame.
    u32 SwitchBootHost::BuildYellowColor() {
        return RGBA8_MAXALPHA(255, 255, 0);
    }
}
