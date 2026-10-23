#include "platform/switch/SwitchBootHost.hpp"

#include <cstdio>
#include <cstring>
#include <cstdlib>
#include <exception>
#include <typeinfo>

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include <stdexcept>
#include <string>

#include "Core.hpp"
#include "CoreInitializationOptions.hpp"
#include "HostFileSystemContentStreamSource.hpp"
#include "InputControlId.hpp"
#include "InputDeviceKind.hpp"
#include "InputGamepadButton.hpp"
#include "InputSystem.hpp"
#include "InputControlKind.hpp"
#include "PlatformInfo.hpp"
#include "RuntimeSceneCatalog.hpp"
#include "RuntimeSceneCatalogEntry.hpp"
#include "SceneLoadMode.hpp"
#include "SceneManager.hpp"
#include "StandardPlatformAction.hpp"
#include "StandardPlatformActionBinding.hpp"
#include "StandardPlatformInputConfiguration.hpp"
#include "platform/switch/audio/SwitchAudioBackend.hpp"
#include "platform/switch/SwitchInputBackend.hpp"
#include "platform/switch/SwitchPackagedAssetLoader.hpp"
#include "platform/switch/SwitchRenderManager2D.hpp"
#include "platform/switch/SwitchRenderManager3D.hpp"
#include "platform/switch/SwitchRuntimeDiagnosticsProvider.hpp"
#include "runtime/runtime_scene_catalog_manifest.hpp"
#include "runtime/native_exceptions.hpp"
#include "runtime/runtime_standard_platform_input_manifest.hpp"
#include "runtime/runtime_startup_manifest.hpp"
#endif

namespace helengine::switch_platform {
    namespace {
        constexpr u32 FramebufferWidth = 1280;
        constexpr u32 FramebufferHeight = 720;

        /// Sends bounded startup and failure diagnostics to the SDK debug channel and stderr.
        void WriteRuntimeDiagnostic(const char* stage, const char* detail) {
            char message[1024];
            std::snprintf(message, sizeof(message), "HELENGINE_SWITCH_%s: %s\n", stage, detail);
            svcOutputDebugString(message, std::strlen(message));
            std::fputs(message, stderr);
        }

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
        /// Preserves the platform's original fatal termination behavior after writing diagnostics.
        std::terminate_handler PreviousTerminateHandler = nullptr;

        /// Reports uncaught C++ termination and delegates to the previously installed handler.
        [[noreturn]] void ReportRuntimeTermination() {
            const std::exception_ptr error = std::current_exception();
            if (error != nullptr) {
                try {
                    std::rethrow_exception(error);
                } catch (const ::Exception* exception) {
                    WriteRuntimeDiagnostic("TERMINATE_MANAGED_TYPE", exception == nullptr ? "null Exception pointer" : typeid(*exception).name());
                    WriteRuntimeDiagnostic("TERMINATE_MANAGED", exception == nullptr ? "null Exception pointer" : exception->what());
                } catch (const std::exception& exception) {
                    WriteRuntimeDiagnostic("TERMINATE", exception.what());
                } catch (...) {
                    WriteRuntimeDiagnostic("TERMINATE", "non-standard exception");
                }
            } else {
                WriteRuntimeDiagnostic("TERMINATE", "no active C++ exception");
            }
            if (PreviousTerminateHandler != nullptr) {
                PreviousTerminateHandler();
            }
            std::abort();
        }

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
        , EngineAudioBackend(nullptr)
        , EnginePlatformInfo(nullptr)
#endif
    {
    }

    /// Initializes the Switch boot path and keeps frames visible until shutdown.
    int SwitchBootHost::Run() {
#if HELENGINE_SWITCH_HAS_GENERATED_CORE
        PreviousTerminateHandler = std::set_terminate(ReportRuntimeTermination);
#endif
        if (!InitializeFramebuffer()) {
#if HELENGINE_SWITCH_HAS_GENERATED_CORE
            std::set_terminate(PreviousTerminateHandler);
#endif
            return 1;
        }

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
        try {
            WriteRuntimeDiagnostic("BOOT", "framebuffer ready; mounting application RomFS");
            if (MountRomFs()) {
                WriteRuntimeDiagnostic("BOOT", "initializing Core");
                InitializeCore();
                WriteRuntimeDiagnostic("BOOT", "Core initialized; loading startup scene");
                LoadStartupScene();
                RuntimeReady = true;
                CurrentFrameColor = BuildRuntimeReadyColor();
                WriteRuntimeDiagnostic("BOOT", "startup complete");
            } else {
                CurrentFrameColor = BuildFailureColor();
            }
        } catch (const std::exception& error) {
            RuntimeReady = false;
            CurrentFrameColor = BuildFailureColor();
            WriteRuntimeDiagnostic("BOOT_FATAL", error.what());
        }
#endif

        const int result = RunMainLoop();
#if HELENGINE_SWITCH_HAS_GENERATED_CORE
        std::set_terminate(PreviousTerminateHandler);
#endif
        return result;
    }

    /// Runs the steady-state Switch frame loop.
    int SwitchBootHost::RunMainLoop() {
        while (appletMainLoop()) {
            UpdateRuntimeFrame();
            RenderFrame();
        }
        WriteRuntimeDiagnostic("APPLET_LOOP", "appletMainLoop returned false; shutting down");

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
        ShutdownCore();
#endif
        if (HasMountedRomFs) {
            romfsExit();
        }
        if (HasFramebuffer) {
            framebufferClose(&FramebufferState);
        }

        return RuntimeReady ? 0 : 1;
    }

    /// Mounts the application's embedded NRO RomFS or current-process NSO storage through libnx.
    bool SwitchBootHost::MountRomFs() {
        const Result result = romfsInit();
        char detail[128];
        std::snprintf(detail, sizeof(detail), "romfsInit=0x%08x envIsNso=%d", static_cast<unsigned int>(result), envIsNso());
        WriteRuntimeDiagnostic("ROMFS", detail);
        if (R_FAILED(result)) {
            return false;
        }

        HasMountedRomFs = true;
        return true;
    }

    /// Initializes the generated-core runtime when generated output is available.
    void SwitchBootHost::InitializeCore() {
#if HELENGINE_SWITCH_HAS_GENERATED_CORE
        EngineCore = new ::Core();
        EngineOptions = EngineCore->get_InitializationOptions();
        static SwitchRuntimeDiagnosticsProvider startupDiagnostics;
        EngineOptions->set_RuntimeDiagnosticsProvider(&startupDiagnostics);
        EngineOptions->set_ContentStreamSource(new HostFileSystemContentStreamSource("romfs:/"));
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
        EngineAudioBackend = new SwitchAudioBackend();
        EnginePlatformInfo = new ::PlatformInfo("switch", "1.0.0");
        EngineRenderManager3D->AddWindow(0, FramebufferWidth, FramebufferHeight);
        EngineCore->Initialize(
            EngineRenderManager3D,
            EngineRenderManager2D,
            EngineInputBackend,
            EnginePlatformInfo,
            EngineOptions);
        EngineCore->SetAudioBackend(EngineAudioBackend);
#endif
    }

    /// Loads the configured startup scene from RomFS through the runtime scene manager.
    void SwitchBootHost::LoadStartupScene() {
#if HELENGINE_SWITCH_HAS_GENERATED_CORE
        SwitchPackagedAssetLoader packagedAssetLoader("romfs:/");
        const char* startupSceneRelativePath = he_get_runtime_startup_scene_relative_path();
        if (startupSceneRelativePath == nullptr || startupSceneRelativePath[0] == '\0') {
            WriteRuntimeDiagnostic("STARTUP_SCENE", "manifest contains no startup scene path");
            return;
        }

        if (!packagedAssetLoader.AssetExists(startupSceneRelativePath)) {
            WriteRuntimeDiagnostic("STARTUP_SCENE_MISSING", startupSceneRelativePath);
            return;
        }

        std::string startupSceneId = ResolveStartupSceneId(startupSceneRelativePath);
        if (startupSceneId.empty()) {
            WriteRuntimeDiagnostic("STARTUP_SCENE_UNRESOLVED", startupSceneRelativePath);
            return;
        }

        EngineCore->get_SceneManager()->LoadScene(startupSceneId, SceneLoadMode::Single);
        WriteRuntimeDiagnostic("MENU_LOADED", startupSceneId.c_str());
#endif
    }

    /// Updates the generated-core runtime for one frame when available.
    void SwitchBootHost::UpdateRuntimeFrame() {
#if HELENGINE_SWITCH_HAS_GENERATED_CORE
        if (RuntimeReady && EngineCore != nullptr) {
            static bool firstUpdate = true;
            try {
                if (firstUpdate) {
                    WriteRuntimeDiagnostic("UPDATE", "first update starting");
                }
                EngineCore->Update();
                if (firstUpdate) {
                    WriteRuntimeDiagnostic("UPDATE", "first update complete");
                    firstUpdate = false;
                }
            } catch (const std::exception& error) {
                RuntimeReady = false;
                CurrentFrameColor = BuildFailureColor();
                WriteRuntimeDiagnostic("UPDATE_FATAL", error.what());
            } catch (...) {
                RuntimeReady = false;
                CurrentFrameColor = BuildFailureColor();
                WriteRuntimeDiagnostic("UPDATE_FATAL", "unknown exception");
            }
        }
#endif
    }

    /// Releases native runtime services that require explicit shutdown.
    void SwitchBootHost::ShutdownCore() {
#if HELENGINE_SWITCH_HAS_GENERATED_CORE
        if (EngineAudioBackend != nullptr) {
            delete EngineAudioBackend;
            EngineAudioBackend = nullptr;
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

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
        if(RuntimeReady && EngineCore!=nullptr) {
            auto* renderer=static_cast<SwitchRenderManager2D*>(EngineRenderManager2D);
            renderer->BeginFrame(pixels,FramebufferWidth,FramebufferHeight,stridePixels);
            static bool firstRender = true;
            try {
                if (firstRender) {
                    WriteRuntimeDiagnostic("RENDER", "first draw starting");
                }
                EngineCore->Draw();
                if (firstRender) {
                    WriteRuntimeDiagnostic("RENDER", "Core.Draw returned; renderer draw starting");
                }
                renderer->Draw();
                if (firstRender) {
                    WriteRuntimeDiagnostic("RENDER", "renderer draw returned; ending frame");
                }
                renderer->EndFrame();
                if (firstRender) {
                    WriteRuntimeDiagnostic("RENDER", "first draw complete");
                    firstRender = false;
                }
                static uint64_t frame=0;
                if(frame++==0 || frame%600==0) std::printf("HELENGINE_SWITCH_MENU_FRAME: %llu blended_samples=%llu\n",static_cast<unsigned long long>(frame),static_cast<unsigned long long>(renderer->GetWrittenPixels()));
            }
            catch (const ::Exception* error) {
                WriteRuntimeDiagnostic("RENDER_MANAGED_TYPE", error == nullptr ? "null Exception pointer" : typeid(*error).name());
                WriteRuntimeDiagnostic("RENDER_MANAGED_FATAL", error == nullptr ? "null Exception pointer" : error->what());
                throw;
            }
            catch(const std::exception& error) {
                renderer->EndFrame(); RuntimeReady=false; CurrentFrameColor=BuildFailureColor();
                WriteRuntimeDiagnostic("RENDER_FATAL", error.what());
            } catch (...) {
                WriteRuntimeDiagnostic("RENDER_FATAL", "non-standard exception; preserving propagation");
                throw;
            }
        }
#endif

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

/// Observes the SDK's existing application-exit cleanup without replacing its termination path.
extern "C" void userAppExit() {
    constexpr char message[] = "HELENGINE_SWITCH_APP_EXIT: libnx application cleanup invoked\n";
    svcOutputDebugString(message, sizeof(message) - 1);
}
