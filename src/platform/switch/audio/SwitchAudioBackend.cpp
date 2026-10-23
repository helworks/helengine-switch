#include "platform/switch/audio/SwitchAudioBackend.hpp"

#include <algorithm>
#include <cctype>
#include <cstdlib>
#include <cstring>
#include <malloc.h>
#include <stdexcept>
#include <utility>

#define Event LibnxEvent
#include <switch.h>
#undef Event

namespace helengine::switch_platform {
    namespace {
        constexpr int32_t OutputChannelCount = 2;

        void ConfigureVoiceMix(AudioDriver& driver, int32_t voiceSlot, int32_t channelCount) {
            audrvVoiceSetDestinationMix(&driver, voiceSlot, AUDREN_FINAL_MIX_ID);

            if (channelCount <= 1) {
                audrvVoiceSetMixFactor(&driver, voiceSlot, 1.0f, 0, 0);
                audrvVoiceSetMixFactor(&driver, voiceSlot, 1.0f, 0, 1);
                return;
            }

            audrvVoiceSetMixFactor(&driver, voiceSlot, 1.0f, 0, 0);
            audrvVoiceSetMixFactor(&driver, voiceSlot, 0.0f, 0, 1);
            audrvVoiceSetMixFactor(&driver, voiceSlot, 0.0f, 1, 0);
            audrvVoiceSetMixFactor(&driver, voiceSlot, 1.0f, 1, 1);
        }
    }

    struct SwitchAudioBackend::NativeState {
        AudioRendererConfig RendererConfig;
        AudioDriver Driver;
        bool AudrenInitialized;
        bool DriverInitialized;

        NativeState()
            : RendererConfig()
            , Driver()
            , AudrenInitialized(false)
            , DriverInitialized(false) {
            RendererConfig.output_rate = AudioRendererOutputRate_48kHz;
            RendererConfig.num_voices = MaxVoices;
            RendererConfig.num_effects = 0;
            RendererConfig.num_sinks = 1;
            RendererConfig.num_mix_objs = 1;
            RendererConfig.num_mix_buffers = OutputChannelCount;
        }
    };

    SwitchAudioBackend::SwitchAudioBackend()
        : NextVoiceId(0)
        , State(new NativeState())
        , VoiceSlotsInUse()
        , ActiveVoicesById()
        , BusGainsById()
        , PausedBusIds() {
        BusGainsById.emplace("master", 1.0f);
        BusGainsById.emplace("music", 1.0f);
        BusGainsById.emplace("sfx", 1.0f);

        try {
            Result initializeResult = audrenInitialize(&State->RendererConfig);
            if (R_FAILED(initializeResult)) {
                throw std::runtime_error("Switch audio playback could not initialize audren.");
            }

            State->AudrenInitialized = true;

            Result driverCreateResult = audrvCreate(&State->Driver, &State->RendererConfig, OutputChannelCount);
            if (R_FAILED(driverCreateResult)) {
                throw std::runtime_error("Switch audio playback could not create the audio driver.");
            }

            State->DriverInitialized = true;

            static const u8 sinkChannels[OutputChannelCount] = { 0, 1 };
            if (audrvDeviceSinkAdd(&State->Driver, AUDREN_DEFAULT_DEVICE_NAME, OutputChannelCount, sinkChannels) < 0) {
                throw std::runtime_error("Switch audio playback could not create the default device sink.");
            }

            CommitDriverState();

            Result startResult = audrenStartAudioRenderer();
            if (R_FAILED(startResult)) {
                throw std::runtime_error("Switch audio playback could not start the audio renderer.");
            }
        }
        catch (...) {
            if (State != nullptr) {
                if (State->DriverInitialized) {
                    audrvClose(&State->Driver);
                    State->DriverInitialized = false;
                }

                if (State->AudrenInitialized) {
                    audrenExit();
                    State->AudrenInitialized = false;
                }

                delete State;
                State = nullptr;
            }

            throw;
        }
    }

    SwitchAudioBackend::~SwitchAudioBackend() {
        if (State == nullptr) {
            return;
        }

        for (auto& voiceEntry : ActiveVoicesById) {
            try {
                ReleaseVoiceState(voiceEntry.second);
            }
            catch (...) {
            }
        }

        ActiveVoicesById.clear();

        if (State->DriverInitialized) {
            audrvClose(&State->Driver);
            State->DriverInitialized = false;
        }

        if (State->AudrenInitialized) {
            audrenExit();
            State->AudrenInitialized = false;
        }

        delete State;
        State = nullptr;
    }

    int32_t SwitchAudioBackend::Play(::AudioAsset* asset, ::AudioPlaybackRequest* request) {
        if (asset == nullptr) {
            throw std::invalid_argument("asset");
        }
        if (asset->SampleRate <= 0) {
            throw std::runtime_error("Switch audio playback requires a positive sample rate.");
        }
        if (asset->EncodingFamilyId != "pcm-streamed") {
            throw std::runtime_error("Switch audio playback currently requires shared pcm-streamed assets.");
        }
        if (asset->Channels <= 0 || asset->Channels > OutputChannelCount) {
            throw std::runtime_error("Switch audio playback currently supports only mono or stereo 16-bit PCM assets.");
        }
        if (asset->EncodedBytes == nullptr || asset->EncodedBytes->Length <= 0 || asset->EncodedBytes->Data == nullptr) {
            throw std::runtime_error("Switch audio playback requires one non-empty encoded payload.");
        }
        if ((asset->EncodedBytes->Length % static_cast<int32_t>(sizeof(std::int16_t) * asset->Channels)) != 0) {
            throw std::runtime_error("Switch audio playback requires 16-bit PCM sample alignment.");
        }

        const int32_t sampleFrameCount =
            asset->EncodedBytes->Length / static_cast<int32_t>(sizeof(std::int16_t) * asset->Channels);
        if (sampleFrameCount <= 0) {
            throw std::runtime_error("Switch audio playback requires one non-empty PCM frame payload.");
        }

        const int32_t voiceSlot = AcquireVoiceSlot();
        if (voiceSlot < 0) {
            throw std::runtime_error("Switch audio playback could not reserve one audren voice.");
        }

        ActiveVoiceState voice = {};
        voice.VoiceId = NextVoiceId++;
        voice.VoiceSlot = voiceSlot;
        voice.ChannelCount = asset->Channels;
        voice.BusId = NormalizeBusId(
            request != nullptr && !request->BusId.empty()
                ? request->BusId
                : asset->DefaultBusId);
        voice.BaseGain = ClampGain(request != nullptr ? request->Gain : 1.0f);
        voice.Looping = request != nullptr ? request->Loop : asset->DefaultLoop;
        voice.Paused = false;
        voice.AudioBuffer = nullptr;
        voice.AudioBufferByteLength = 0;
        voice.MemoryPoolId = -1;
        voice.WaveBuffer = nullptr;

        try {
            const std::size_t alignedByteLength = AlignUp(
                static_cast<std::size_t>(asset->EncodedBytes->Length),
                RequiredAudioAlignment);
            void* audioBuffer = memalign(RequiredAudioAlignment, alignedByteLength);
            if (audioBuffer == nullptr) {
                throw std::runtime_error("Switch audio playback could not allocate one aligned PCM buffer.");
            }

            std::memset(audioBuffer, 0, alignedByteLength);
            std::memcpy(audioBuffer, asset->EncodedBytes->Data, static_cast<std::size_t>(asset->EncodedBytes->Length));
            armDCacheFlush(audioBuffer, alignedByteLength);
            voice.AudioBuffer = audioBuffer;
            voice.AudioBufferByteLength = alignedByteLength;

            const int memoryPoolId = audrvMemPoolAdd(&State->Driver, audioBuffer, alignedByteLength);
            if (memoryPoolId < 0) {
                throw std::runtime_error("Switch audio playback could not register one audren mempool.");
            }

            voice.MemoryPoolId = memoryPoolId;
            if (!audrvMemPoolAttach(&State->Driver, voice.MemoryPoolId)) {
                throw std::runtime_error("Switch audio playback could not attach one audren mempool.");
            }

            if (!audrvVoiceInit(&State->Driver, voice.VoiceSlot, voice.ChannelCount, PcmFormat_Int16, asset->SampleRate)) {
                throw std::runtime_error("Switch audio playback could not initialize one audren voice.");
            }

            ConfigureVoiceMix(State->Driver, voice.VoiceSlot, voice.ChannelCount);
            audrvVoiceSetVolume(&State->Driver, voice.VoiceSlot, ResolveCombinedGain(voice.BusId, voice.BaseGain));
            audrvVoiceStart(&State->Driver, voice.VoiceSlot);

            AudioDriverWaveBuf* waveBuffer = new AudioDriverWaveBuf();
            std::memset(waveBuffer, 0, sizeof(AudioDriverWaveBuf));
            waveBuffer->data_pcm16 = static_cast<std::int16_t*>(voice.AudioBuffer);
            waveBuffer->size = static_cast<u64>(asset->EncodedBytes->Length);
            waveBuffer->start_sample_offset = 0;
            waveBuffer->end_sample_offset = sampleFrameCount;
            waveBuffer->is_looping = voice.Looping;

            if (!audrvVoiceAddWaveBuf(&State->Driver, voice.VoiceSlot, waveBuffer)) {
                delete waveBuffer;
                throw std::runtime_error("Switch audio playback could not queue one audren wave buffer.");
            }

            voice.WaveBuffer = waveBuffer;
            VoiceSlotsInUse[voice.VoiceSlot] = true;
            ActiveVoicesById.emplace(voice.VoiceId, voice);
            ApplyVoicePlaybackState(ActiveVoicesById.at(voice.VoiceId));
            CommitDriverState();
            return voice.VoiceId;
        }
        catch (...) {
            try {
                ReleaseVoiceState(voice);
            }
            catch (...) {
            }

            throw;
        }
    }

    void SwitchAudioBackend::Stop(int32_t voiceId) {
        auto voiceIterator = ActiveVoicesById.find(voiceId);
        if (voiceIterator == ActiveVoicesById.end()) {
            return;
        }

        ReleaseVoiceState(voiceIterator->second);
        ActiveVoicesById.erase(voiceIterator);
    }

    void SwitchAudioBackend::SetBusGain(HeCppString busId, float gain) {
        BusGainsById[NormalizeBusId(std::move(busId))] = ClampGain(gain);
        for (auto& voiceEntry : ActiveVoicesById) {
            ApplyVoicePlaybackState(voiceEntry.second);
        }

        CommitDriverState();
    }

    void SwitchAudioBackend::SetBusPaused(HeCppString busId, bool paused) {
        std::string normalizedBusId = NormalizeBusId(std::move(busId));
        if (paused) {
            PausedBusIds.insert(normalizedBusId);
        } else {
            PausedBusIds.erase(normalizedBusId);
        }

        for (auto& voiceEntry : ActiveVoicesById) {
            ApplyVoicePlaybackState(voiceEntry.second);
        }

        CommitDriverState();
    }

    bool SwitchAudioBackend::IsPlaying(int32_t voiceId) {
        auto voiceIterator = ActiveVoicesById.find(voiceId);
        if (voiceIterator == ActiveVoicesById.end()) {
            return false;
        }

        const ActiveVoiceState& voice = voiceIterator->second;
        if (voice.Paused) {
            return true;
        }

        const AudioDriverWaveBuf* waveBuffer = static_cast<const AudioDriverWaveBuf*>(voice.WaveBuffer);
        if (waveBuffer == nullptr) {
            return false;
        }

        if (voice.Looping) {
            return true;
        }

        return waveBuffer->state != AudioDriverWaveBufState_Done
            && waveBuffer->state != AudioDriverWaveBufState_Free;
    }

    void SwitchAudioBackend::Update() {
        CommitDriverState();

        for (auto voiceIterator = ActiveVoicesById.begin(); voiceIterator != ActiveVoicesById.end();) {
            ActiveVoiceState& voice = voiceIterator->second;
            if (voice.Looping || voice.Paused) {
                ++voiceIterator;
                continue;
            }

            const AudioDriverWaveBuf* waveBuffer = static_cast<const AudioDriverWaveBuf*>(voice.WaveBuffer);
            const bool completed = waveBuffer == nullptr
                || waveBuffer->state == AudioDriverWaveBufState_Done
                || waveBuffer->state == AudioDriverWaveBufState_Free;
            if (!completed) {
                ++voiceIterator;
                continue;
            }

            ReleaseVoiceState(voice);
            voiceIterator = ActiveVoicesById.erase(voiceIterator);
        }
    }

    std::string SwitchAudioBackend::NormalizeBusId(HeCppString busId) {
        if (busId.empty()) {
            return "master";
        }

        std::string normalizedBusId = busId.raw();
        std::transform(
            normalizedBusId.begin(),
            normalizedBusId.end(),
            normalizedBusId.begin(),
            [](unsigned char value) {
                return static_cast<char>(std::tolower(value));
            });
        return normalizedBusId;
    }

    float SwitchAudioBackend::ClampGain(float gain) {
        if (!(gain >= 0.0f) || gain != gain) {
            return 0.0f;
        }

        return std::clamp(gain, 0.0f, 1.0f);
    }

    std::size_t SwitchAudioBackend::AlignUp(std::size_t value, std::size_t alignment) {
        if (alignment == 0) {
            return value;
        }

        return ((value + alignment - 1) / alignment) * alignment;
    }

    int32_t SwitchAudioBackend::AcquireVoiceSlot() const {
        for (int32_t voiceSlot = 0; voiceSlot < MaxVoices; voiceSlot++) {
            if (!VoiceSlotsInUse[voiceSlot]) {
                return voiceSlot;
            }
        }

        return -1;
    }

    float SwitchAudioBackend::ResolveCombinedGain(const std::string& busId, float baseGain) const {
        float masterGain = 1.0f;
        auto masterGainIterator = BusGainsById.find("master");
        if (masterGainIterator != BusGainsById.end()) {
            masterGain = masterGainIterator->second;
        }

        float busGain = 1.0f;
        auto busGainIterator = BusGainsById.find(busId);
        if (busGainIterator != BusGainsById.end()) {
            busGain = busGainIterator->second;
        }

        return ClampGain(masterGain * busGain * baseGain);
    }

    bool SwitchAudioBackend::IsBusPaused(const std::string& busId) const {
        return PausedBusIds.contains("master") || PausedBusIds.contains(busId);
    }

    void SwitchAudioBackend::ApplyVoicePlaybackState(ActiveVoiceState& voice) {
        const bool shouldPause = IsBusPaused(voice.BusId);
        audrvVoiceSetVolume(&State->Driver, voice.VoiceSlot, ResolveCombinedGain(voice.BusId, voice.BaseGain));
        audrvVoiceSetPaused(&State->Driver, voice.VoiceSlot, shouldPause);
        voice.Paused = shouldPause;
    }

    void SwitchAudioBackend::ReleaseVoiceState(ActiveVoiceState& voice) {
        if (State == nullptr || !State->DriverInitialized) {
            return;
        }

        if (voice.VoiceSlot >= 0 && voice.VoiceSlot < MaxVoices) {
            audrvVoiceStop(&State->Driver, voice.VoiceSlot);
            audrvVoiceDrop(&State->Driver, voice.VoiceSlot);
            VoiceSlotsInUse[voice.VoiceSlot] = false;
        }

        if (voice.MemoryPoolId >= 0) {
            audrvMemPoolDetach(&State->Driver, voice.MemoryPoolId);
            audrvMemPoolRemove(&State->Driver, voice.MemoryPoolId);
        }

        CommitDriverState();

        if (voice.WaveBuffer != nullptr) {
            delete static_cast<AudioDriverWaveBuf*>(voice.WaveBuffer);
            voice.WaveBuffer = nullptr;
        }

        if (voice.AudioBuffer != nullptr) {
            std::free(voice.AudioBuffer);
            voice.AudioBuffer = nullptr;
        }

        voice.AudioBufferByteLength = 0;
        voice.MemoryPoolId = -1;
        voice.ChannelCount = 0;
        voice.Paused = false;
        voice.Looping = false;
        voice.VoiceSlot = -1;
    }

    void SwitchAudioBackend::CommitDriverState() {
        if (State == nullptr || !State->DriverInitialized) {
            return;
        }

        Result updateResult = audrvUpdate(&State->Driver);
        if (R_FAILED(updateResult)) {
            throw std::runtime_error("Switch audio playback failed to commit one audren driver update.");
        }
    }
}
