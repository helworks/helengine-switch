#pragma once

#define Event LibnxEvent
#include <switch.h>
#undef Event

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
class Core;
class CoreInitializationOptions;
class IInputBackend;
class PlatformInfo;
class RenderManager2D;
class RenderManager3D;
#endif

namespace helengine::switch_platform {
    class SwitchAudioBackend;
}

namespace helengine::switch_platform {
    /// Owns the Switch native framebuffer bootstrap and generated-core-aware presentation loop.
    class SwitchBootHost {
    public:
        /// Creates one Switch boot host with no initialized runtime state.
        SwitchBootHost();

        /// Initializes the Switch boot path and keeps frames visible until shutdown.
        int Run();

    private:
        /// Initializes the generated-core runtime when generated output is available.
        void InitializeCore();

        /// Loads the configured startup scene from RomFS through the runtime scene manager.
        void LoadStartupScene();

        /// Runs the steady-state Switch frame loop.
        int RunMainLoop();

        /// Mounts the packaged RomFS image from the current process.
        bool MountRomFs();

        /// Updates the generated-core runtime for one frame when available.
        void UpdateRuntimeFrame();

        /// Releases native runtime services that require explicit shutdown.
        void ShutdownCore();

        /// Renders one bootstrap or runtime frame into the active framebuffer.
        void RenderFrame();

        /// Initializes a linear framebuffer bound to the default Switch window.
        bool InitializeFramebuffer();

        /// Renders one full solid-color frame into the active framebuffer.
        void RenderSolidColorFrame(u32 color);

        /// Resolves the clear color used while the runtime is running successfully.
        static u32 BuildRuntimeReadyColor();

        /// Resolves the clear color used when runtime startup or update fails.
        static u32 BuildFailureColor();

        /// Builds the packed RGBA color value used for the first visible yellow frame.
        static u32 BuildBootstrapColor();

        /// Stores the Switch framebuffer used for bootstrap and runtime clear passes.
        Framebuffer FramebufferState;

        /// Stores whether the framebuffer was initialized successfully.
        bool HasFramebuffer;

        /// Stores whether RomFS was mounted successfully for generated-core startup.
        bool HasMountedRomFs;

        /// Stores whether the generated-core runtime completed startup successfully.
        bool RuntimeReady;

        /// Stores the current clear color used by the visible frame loop.
        u32 CurrentFrameColor;

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
        /// Stores the generated-core runtime instance.
        ::Core* EngineCore;

        /// Stores the runtime initialization options owned by the generated core.
        ::CoreInitializationOptions* EngineOptions;

        /// Stores the Switch 3D render manager placeholder used by generated core.
        ::RenderManager3D* EngineRenderManager3D;

        /// Stores the Switch 2D render manager placeholder used by generated core.
        ::RenderManager2D* EngineRenderManager2D;

        /// Stores the Switch input backend used by generated core.
        ::IInputBackend* EngineInputBackend;

        /// Stores the Switch audio backend used by generated core scene audio sources.
        SwitchAudioBackend* EngineAudioBackend;

        /// Stores the runtime platform metadata reported to generated core.
        ::PlatformInfo* EnginePlatformInfo;
#endif
    };
}
