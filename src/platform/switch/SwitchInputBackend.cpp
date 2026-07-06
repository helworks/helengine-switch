#include "platform/switch/SwitchInputBackend.hpp"

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#define Event LibnxEvent
#include <switch.h>
#undef Event

#include "InputFrameState.hpp"
#include "InputGamepadButton.hpp"
#include "InputGamepadState.hpp"
#include "runtime/array.hpp"

namespace helengine::switch_platform {
    /// Creates the Switch input backend.
    SwitchInputBackend::SwitchInputBackend() {
    }

    /// Captures one input frame from the Switch runtime.
    ::InputFrameState SwitchInputBackend::CaptureFrame() {
        padConfigureInput(1, HidNpadStyleSet_NpadStandard);

        PadState padState;
        padInitializeDefault(&padState);
        padUpdate(&padState);

        u64 buttons = padGetButtons(&padState);
        HidAnalogStickState leftStick = padGetStickPos(&padState, 0);

        ::InputFrameState frameState;
        frameState.set_GamepadCount(1);

        ::Array<::InputGamepadState>* gamepads = new ::Array<::InputGamepadState>(1);
        ::InputGamepadState gamepadState;
        gamepadState.set_Connected(true);
        gamepadState.SetButtonDown(::InputGamepadButton::DPadUp, (buttons & HidNpadButton_Up) != 0);
        gamepadState.SetButtonDown(::InputGamepadButton::DPadDown, (buttons & HidNpadButton_Down) != 0);
        gamepadState.SetButtonDown(::InputGamepadButton::DPadLeft, (buttons & HidNpadButton_Left) != 0);
        gamepadState.SetButtonDown(::InputGamepadButton::DPadRight, (buttons & HidNpadButton_Right) != 0);
        gamepadState.SetButtonDown(::InputGamepadButton::South, (buttons & HidNpadButton_A) != 0);
        gamepadState.SetButtonDown(::InputGamepadButton::East, (buttons & HidNpadButton_B) != 0);
        gamepadState.SetButtonDown(::InputGamepadButton::West, (buttons & HidNpadButton_Y) != 0);
        gamepadState.SetButtonDown(::InputGamepadButton::North, (buttons & HidNpadButton_X) != 0);
        gamepadState.SetButtonDown(::InputGamepadButton::LeftShoulder, (buttons & HidNpadButton_L) != 0);
        gamepadState.SetButtonDown(::InputGamepadButton::RightShoulder, (buttons & HidNpadButton_R) != 0);
        gamepadState.SetButtonDown(::InputGamepadButton::Start, (buttons & HidNpadButton_Plus) != 0);
        gamepadState.SetButtonDown(::InputGamepadButton::Select, (buttons & HidNpadButton_Minus) != 0);
        gamepadState.set_LeftStickX(static_cast<int16_t>(leftStick.x));
        gamepadState.set_LeftStickY(static_cast<int16_t>(leftStick.y));

        (*gamepads)[0] = gamepadState;
        frameState.set_Gamepads(gamepads);
        return frameState;
    }
}
#endif
