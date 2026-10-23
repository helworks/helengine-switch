#include "platform/switch/SwitchRuntimeDiagnosticsProvider.hpp"

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include <cstdio>
#include <cstring>
#define Event LibnxEvent
#include <switch.h>
#undef Event
#include "RuntimeMemoryDiagnosticsSnapshot.hpp"

namespace helengine::switch_platform {
    /// Captures assigned process memory without inferring unsupported resident-memory counters.
    ::RuntimeMemoryDiagnosticsSnapshot* SwitchRuntimeDiagnosticsProvider::CaptureSnapshot() {
        auto* snapshot = new ::RuntimeMemoryDiagnosticsSnapshot();
        u64 usedMemory = 0;
        if (R_SUCCEEDED(svcGetInfo(&usedMemory, InfoType_UsedMemorySize, CUR_PROCESS_HANDLE, 0))) {
            snapshot->set_CommittedBytes(usedMemory);
        }
        return snapshot;
    }

    /// Reports the existing scene-manager stage and stops once the first commit has returned.
    void SwitchRuntimeDiagnosticsProvider::ReportSceneTransitionStage(HeCppString stage, HeCppString sceneId, int32_t loadedSceneCount, int32_t pendingOperationCount) {
        char detail[512];
        const char* stageText = stage.c_str();
        const char* sceneText = sceneId.c_str();
        std::snprintf(detail, sizeof(detail), "%s scene=%s loaded=%d pending=%d", stageText == nullptr ? "<null>" : stageText, sceneText == nullptr ? "<null>" : sceneText, loadedSceneCount, pendingOperationCount);
        WriteStage("SCENE_STAGE", detail);
        if (stage == HeCppString("CommitPendingOperationsAtFrameBoundaryEnd")) {
            FirstCommitCompleted = true;
        }
    }

    /// Reports the existing Core stages while the initial deferred scene load is pending.
    void SwitchRuntimeDiagnosticsProvider::ReportUpdateStage(HeCppString stage) {
        WriteStage("CORE_STAGE", stage.c_str());
    }

    /// Sends at most 96 startup messages through the SDK debug channel used by Eden.
    void SwitchRuntimeDiagnosticsProvider::WriteStage(const char* stage, const char* detail) {
        if (FirstCommitCompleted || RemainingReports <= 0) {
            return;
        }
        --RemainingReports;
        char message[768];
        std::snprintf(message, sizeof(message), "HELENGINE_SWITCH_%s: %s\n", stage == nullptr ? "<null>" : stage, detail == nullptr ? "<null>" : detail);
        svcOutputDebugString(message, std::strlen(message));
    }
}
#endif
