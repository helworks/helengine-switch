#pragma once

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include "IRuntimeDiagnosticsProvider.hpp"
#include "IRuntimeSceneTransitionDiagnosticsProvider.hpp"
#include "IRuntimeUpdateStageDiagnosticsProvider.hpp"

namespace helengine::switch_platform {
    /// Reports bounded startup stages through the existing Core diagnostics contracts.
    class SwitchRuntimeDiagnosticsProvider final : public ::IRuntimeDiagnosticsProvider,
        public ::IRuntimeSceneTransitionDiagnosticsProvider,
        public ::IRuntimeUpdateStageDiagnosticsProvider {
    public:
        /// Captures the process memory currently assigned by the Switch kernel.
        ::RuntimeMemoryDiagnosticsSnapshot* CaptureSnapshot() override;

        /// Reports scene materialization boundaries until the first pending-operation commit completes.
        void ReportSceneTransitionStage(HeCppString stage, HeCppString sceneId, int32_t loadedSceneCount, int32_t pendingOperationCount) override;

        /// Reports Core boundaries only while the first scene commit is being diagnosed.
        void ReportUpdateStage(HeCppString stage) override;

    private:
        /// Emits one bounded message and consumes the startup-only report allowance.
        void WriteStage(const char* stage, const char* detail);

        /// Caps startup diagnostics even when scene materialization cannot finish.
        int RemainingReports = 96;

        /// Stops subsequent frame diagnostics after the first scene commit returns.
        bool FirstCommitCompleted = false;
    };
}
#endif
