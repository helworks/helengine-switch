#pragma once

#include <array>
#include <cstddef>
#include <cstdint>
#include <string>
#include <unordered_map>
#include <unordered_set>

#include "AudioAsset.hpp"
#include "AudioPlaybackRequest.hpp"
#include "IAudioBackend.hpp"

namespace helengine::switch_platform {
    /// <summary>
    /// Plays shared Helengine PCM assets through the Switch audren driver.
    /// </summary>
    class SwitchAudioBackend final : public ::IAudioBackend {
    public:
        /// <summary>
        /// Initializes the Switch audio renderer, device sink, and default bus gains.
        /// </summary>
        SwitchAudioBackend();

        /// <summary>
        /// Stops active voices and tears down the Switch audio renderer.
        /// </summary>
        ~SwitchAudioBackend();

        int32_t Play(::AudioAsset* asset, ::AudioPlaybackRequest* request) override;

        void Stop(int32_t voiceId) override;

        void SetBusGain(std::string busId, float gain) override;

        void SetBusPaused(std::string busId, bool paused) override;

        bool IsPlaying(int32_t voiceId) override;

        void Update() override;

    private:
        static constexpr int32_t MaxVoices = 24;
        static constexpr std::size_t RequiredAudioAlignment = 0x1000;

        struct ActiveVoiceState {
            /// <summary>
            /// Stores the backend-owned voice identifier returned to generated core.
            /// </summary>
            int32_t VoiceId;

            /// <summary>
            /// Stores the reserved audren voice slot used for playback.
            /// </summary>
            int32_t VoiceSlot;

            /// <summary>
            /// Stores the cooked asset channel count bound to the native voice.
            /// </summary>
            int32_t ChannelCount;

            /// <summary>
            /// Stores the normalized mixer bus identifier controlling this voice.
            /// </summary>
            std::string BusId;

            /// <summary>
            /// Stores the per-play request gain applied before bus gain composition.
            /// </summary>
            float BaseGain;

            /// <summary>
            /// Stores whether the queued wave buffer loops continuously.
            /// </summary>
            bool Looping;

            /// <summary>
            /// Stores whether the voice is currently paused by bus state.
            /// </summary>
            bool Paused;

            /// <summary>
            /// Stores the aligned native PCM buffer registered with audren.
            /// </summary>
            void* AudioBuffer;

            /// <summary>
            /// Stores the aligned PCM buffer size registered with the mempool.
            /// </summary>
            std::size_t AudioBufferByteLength;

            /// <summary>
            /// Stores the audren mempool identifier backing the queued PCM data.
            /// </summary>
            int32_t MemoryPoolId;

            /// <summary>
            /// Stores one heap-allocated AudioDriverWaveBuf owned by this voice.
            /// </summary>
            void* WaveBuffer;
        };

        struct NativeState;

        static std::string NormalizeBusId(std::string busId);

        static float ClampGain(float gain);

        static std::size_t AlignUp(std::size_t value, std::size_t alignment);

        int32_t AcquireVoiceSlot() const;

        float ResolveCombinedGain(const std::string& busId, float baseGain) const;

        bool IsBusPaused(const std::string& busId) const;

        void ApplyVoicePlaybackState(ActiveVoiceState& voice);

        void ReleaseVoiceState(ActiveVoiceState& voice);

        void CommitDriverState();

        /// <summary>
        /// Stores the next backend-owned voice identifier.
        /// </summary>
        int32_t NextVoiceId;

        /// <summary>
        /// Stores the Switch audren state hidden from the public header.
        /// </summary>
        NativeState* State;

        /// <summary>
        /// Tracks which audren voice slots are currently reserved.
        /// </summary>
        std::array<bool, MaxVoices> VoiceSlotsInUse;

        /// <summary>
        /// Stores all active backend voices keyed by backend voice identifier.
        /// </summary>
        std::unordered_map<int32_t, ActiveVoiceState> ActiveVoicesById;

        /// <summary>
        /// Stores the current linear gain per mixer bus.
        /// </summary>
        std::unordered_map<std::string, float> BusGainsById;

        /// <summary>
        /// Stores which mixer buses are currently paused.
        /// </summary>
        std::unordered_set<std::string> PausedBusIds;
    };
}
