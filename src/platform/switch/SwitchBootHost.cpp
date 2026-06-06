#include "platform/switch/SwitchBootHost.hpp"

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include <stdexcept>
#include <string>

#include "Core.hpp"
#include "CoreInitializationOptions.hpp"
#include "InputControlId.hpp"
#include "InputDeviceKind.hpp"
#include "InputGamepadButton.hpp"
#include "InputSystem.hpp"
#include "InputControlKind.hpp"
#include "PlatformInfo.hpp"
#include "SceneLoadMode.hpp"
#include "SceneManager.hpp"
#include "StandardPlatformAction.hpp"
#include "StandardPlatformActionBinding.hpp"
#include "StandardPlatformInputConfiguration.hpp"
#include "platform/switch/SwitchInputBackend.hpp"
#include "platform/switch/SwitchPackagedAssetLoader.hpp"
#include "platform/switch/SwitchRenderManager2D.hpp"
#include "platform/switch/SwitchRenderManager3D.hpp"
#include "runtime/runtime_scene_catalog_manifest.hpp"
#include "runtime/runtime_standard_platform_input_manifest.hpp"
#include "runtime/runtime_startup_manifest.hpp"
#endif

namespace helengine::switch_platform {
    namespace {
        constexpr u32 FramebufferWidth = 1280;
        constexpr u32 FramebufferHeight = 720;

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
        /// Builds one runtime scene catalog from the generated native scene-manifest entries.
        ::RuntimeSceneCatalog* BuildRuntimeSceneCatalog() {
            std::size_t sceneCount = 0;
            const HERuntimeSceneCatalogEntry* sceneEntries = he_runtime_scene_catalog_entries(&sceneCount);
            if (sceneEntries == nullptr || sceneCount == 0) {
                return nullptr;
            }

            Array<::RuntimeSceneCatalogEntry*>* catalogEntries = new Array<::RuntimeSceneCatalogEntry*>(static_cast<int32_t>(sceneCount));
            for (std::size_t index = 0; index < sceneCount; index++) {
                const HERuntimeSceneCatalogEntry& sourceEntry = sceneEntries[index];
                (*catalogEntries)[static_cast<int32_t>(index)] = new ::RuntimeSceneCatalogEntry(sourceEntry.SceneId, sourceEntry.CookedRelativePath);
            }

            return new ::RuntimeSceneCatalog(catalogEntries);
        }

        /// Builds one runtime standard-platform-input configuration from the generated manifest entries.
        ::StandardPlatformInputConfiguration* BuildStandardPlatformInputConfiguration() {
            std::size_t entryCount = 0;
            const HERuntimeStandardPlatformActionEntry* actionEntries = he_runtime_standard_platform_action_entries(&entryCount);
            List<::StandardPlatformActionBinding*>* bindings = new List<::StandardPlatformActionBinding*>();
            for (std::size_t index = 0; index < entryCount; index++) {
                const HERuntimeStandardPlatformActionEntry& sourceEntry = actionEntries[index];
                ::InputControlId controlId(
                    static_cast<::InputDeviceKind>(sourceEntry.DeviceKind),
                    static_cast<::InputControlKind>(sourceEntry.ControlKind),
                    sourceEntry.DeviceIndex,
                    sourceEntry.ControlIndex);
                bindings->Add(new ::StandardPlatformActionBinding(
                    static_cast<::StandardPlatformAction>(sourceEntry.ActionId),
                    controlId));
            }

            return new ::StandardPlatformInputConfiguration(bindings);
        }

        /// Resolves the startup scene id from the generated runtime scene-catalog manifest.
        std::string ResolveStartupSceneId(const char* startupSceneRelativePath) {
            std::size_t sceneCount = 0;
            const HERuntimeSceneCatalogEntry* sceneEntries = he_runtime_scene_catalog_entries(&sceneCount);
            for (std::size_t index = 0; index < sceneCount; index++) {
                const HERuntimeSceneCatalogEntry& sourceEntry = sceneEntries[index];
                if (sourceEntry.CookedRelativePath != nullptr && std::string(sourceEntry.CookedRelativePath) == startupSceneRelativePath) {
                    return sourceEntry.SceneId != nullptr ? std::string(sourceEntry.SceneId) : std::string();
                }
            }

            return std::string();
        }
#endif
    }

    /// Creates one Switch boot host with no initialized runtime state.
    SwitchBootHost::SwitchBootHost()
        : HasFramebuffer(false)
        , HasMountedRomFs(false)
        , RuntimeReady(false)
        , CurrentFrameColor(BuildBootstrapColor())
#if HELENGINE_SWITCH_HAS_GENERATED_CORE
        , EngineCore(nullptr)
        , EngineOptions(nullptr)
        , EngineRenderManager3D(nullptr)
        , EngineRenderManager2D(nullptr)
        , EngineInputBackend(nullptr)
        , EnginePlatformInfo(nullptr)
#endif
    {
    }

    /// Initializes the Switch boot path and keeps frames visible until shutdown.
    int SwitchBootHost::Run() {
        if (!InitializeFramebuffer()) {
            return 1;
        }

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
        try {
            if (MountRomFs()) {
                InitializeCore();
                LoadStartupScene();
                RuntimeReady = true;
                CurrentFrameColor = BuildRuntimeReadyColor();
            } else {
                CurrentFrameColor = BuildFailureColor();
            }
        } catch (...) {
            RuntimeReady = false;
            CurrentFrameColor = BuildFailureColor();
        }
#endif

        return RunMainLoop();
    }

    /// Runs the steady-state Switch frame loop.
    int SwitchBootHost::RunMainLoop() {
        while (appletMainLoop()) {
            UpdateRuntimeFrame();
            RenderFrame();
        }

        if (HasMountedRomFs) {
            romfsExit();
        }
        if (HasFramebuffer) {
            framebufferClose(&FramebufferState);
        }

        return RuntimeReady ? 0 : 1;
    }

    /// Mounts the packaged RomFS image from the current process.
    bool SwitchBootHost::MountRomFs() {
        if (R_FAILED(romfsMountFromCurrentProcess("romfs"))) {
            return false;
        }

        HasMountedRomFs = true;
        return true;
    }

    /// Initializes the generated-core runtime when generated output is available.
    void SwitchBootHost::InitializeCore() {
#if HELENGINE_SWITCH_HAS_GENERATED_CORE
        EngineCore = new Core();
        EngineOptions = EngineCore->get_InitializationOptions();
        EngineOptions->set_ContentRootPath("romfs:/");
        EngineOptions->set_UpdateOrderLayers(4);
        EngineOptions->set_RenderOrderLayers3D(4);
        EngineOptions->set_UpdateListInitialCapacity(64);
        EngineOptions->set_RenderList2DInitialCapacity(64);
        EngineOptions->set_RenderList3DInitialCapacity(64);
        EngineOptions->set_SceneCatalog(BuildRuntimeSceneCatalog());
        EngineOptions->set_StandardPlatformInputConfiguration(BuildStandardPlatformInputConfiguration());

        EngineRenderManager3D = new SwitchRenderManager3D();
        EngineRenderManager2D = new SwitchRenderManager2D();
        EngineInputBackend = new SwitchInputBackend();
        EnginePlatformInfo = new PlatformInfo("switch", "1.0.0");
        EngineRenderManager3D->AddWindow(0, FramebufferWidth, FramebufferHeight);
        EngineCore->Initialize(
            EngineRenderManager3D,
            EngineRenderManager2D,
            EngineInputBackend,
            EnginePlatformInfo,
            EngineOptions);
#endif
    }

    /// Loads the configured startup scene from RomFS through the runtime scene manager.
    void SwitchBootHost::LoadStartupScene() {
#if HELENGINE_SWITCH_HAS_GENERATED_CORE
        SwitchPackagedAssetLoader packagedAssetLoader("romfs:/");
        const char* startupSceneRelativePath = he_get_runtime_startup_scene_relative_path();
        if (startupSceneRelativePath == nullptr || startupSceneRelativePath[0] == '\0') {
            return;
        }

        if (!packagedAssetLoader.AssetExists(startupSceneRelativePath)) {
            return;
        }

        std::string startupSceneId = ResolveStartupSceneId(startupSceneRelativePath);
        if (startupSceneId.empty()) {
            return;
        }

        EngineCore->get_SceneManager()->LoadScene(startupSceneId, SceneLoadMode::Single);
#endif
    }

    /// Updates the generated-core runtime for one frame when available.
    void SwitchBootHost::UpdateRuntimeFrame() {
#if HELENGINE_SWITCH_HAS_GENERATED_CORE
        if (RuntimeReady && EngineCore != nullptr) {
            try {
                EngineCore->Update();
            } catch (...) {
                RuntimeReady = false;
                CurrentFrameColor = BuildFailureColor();
            }
        }
#endif
    }

    /// Renders one bootstrap or runtime frame into the active framebuffer.
    void SwitchBootHost::RenderFrame() {
        RenderSolidColorFrame(CurrentFrameColor);
    }

    /// Initializes a linear framebuffer bound to the default Switch window.
    bool SwitchBootHost::InitializeFramebuffer() {
        NWindow* window = nwindowGetDefault();
        if (R_FAILED(framebufferCreate(&FramebufferState, window, FramebufferWidth, FramebufferHeight, PIXEL_FORMAT_RGBA_8888, 2))) {
            return false;
        }

        if (R_FAILED(framebufferMakeLinear(&FramebufferState))) {
            framebufferClose(&FramebufferState);
            return false;
        }

        HasFramebuffer = true;
        return true;
    }

    /// Renders one full solid-color frame into the active framebuffer.
    void SwitchBootHost::RenderSolidColorFrame(u32 color) {
        u32 stride = 0;
        u32* pixels = static_cast<u32*>(framebufferBegin(&FramebufferState, &stride));
        const u32 stridePixels = stride / sizeof(u32);

        for (u32 y = 0; y < FramebufferHeight; ++y) {
            for (u32 x = 0; x < FramebufferWidth; ++x) {
                pixels[y * stridePixels + x] = color;
            }
        }

        framebufferEnd(&FramebufferState);
    }

    /// Resolves the clear color used while the runtime is running successfully.
    u32 SwitchBootHost::BuildRuntimeReadyColor() {
        return RGBA8_MAXALPHA(16, 32, 48);
    }

    /// Resolves the clear color used when runtime startup or update fails.
    u32 SwitchBootHost::BuildFailureColor() {
        return RGBA8_MAXALPHA(160, 16, 16);
    }

    /// Builds the packed RGBA color value used for the first visible yellow frame.
    u32 SwitchBootHost::BuildBootstrapColor() {
        return RGBA8_MAXALPHA(255, 255, 0);
    }
}
