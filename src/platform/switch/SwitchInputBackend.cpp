#include "platform/switch/SwitchInputBackend.hpp"

#if HELENGINE_SWITCH_HAS_GENERATED_CORE
#include <switch.h>

#include "InputFrameState.hpp"
#include "InputGamepadButton.hpp"
#include "InputGamepadState.hpp"
#include "runtime/array.hpp"

namespace helengine::switch_platform {
    /// Creates the Switch input backend.
    SwitchInputBackend::SwitchInputBackend() {
    }

    /// Captures one input frame from the Switch runtime.
    InputFrameState SwitchInputBackend::CaptureFrame() {
        hidScanInput();
        u64 buttons = hidKeysHeld(CONTROLLER_P1_AUTO);
        HidAnalogStickState leftStick = hidJoystickRead(CONTROLLER_P1_AUTO, JOYSTICK_LEFT);

        InputFrameState frameState;
        frameState.set_GamepadCount(1);

        Array<InputGamepadState>* gamepads = new Array<InputGamepadState>(1);
        InputGamepadState gamepadState;
        gamepadState.set_Connected(true);
        gamepadState.SetButtonDown(InputGamepadButton::DPadUp, (buttons & KEY_DUP) != 0);
        gamepadState.SetButtonDown(InputGamepadButton::DPadDown, (buttons & KEY_DDOWN) != 0);
        gamepadState.SetButtonDown(InputGamepadButton::DPadLeft, (buttons & KEY_DLEFT) != 0);
        gamepadState.SetButtonDown(InputGamepadButton::DPadRight, (buttons & KEY_DRIGHT) != 0);
        gamepadState.SetButtonDown(InputGamepadButton::South, (buttons & KEY_A) != 0);
        gamepadState.SetButtonDown(InputGamepadButton::East, (buttons & KEY_B) != 0);
        gamepadState.SetButtonDown(InputGamepadButton::West, (buttons & KEY_Y) != 0);
        gamepadState.SetButtonDown(InputGamepadButton::North, (buttons & KEY_X) != 0);
        gamepadState.SetButtonDown(InputGamepadButton::LeftShoulder, (buttons & KEY_L) != 0);
        gamepadState.SetButtonDown(InputGamepadButton::RightShoulder, (buttons & KEY_R) != 0);
        gamepadState.SetButtonDown(InputGamepadButton::Start, (buttons & KEY_PLUS) != 0);
        gamepadState.SetButtonDown(InputGamepadButton::Select, (buttons & KEY_MINUS) != 0);
        gamepadState.set_LeftStickX(static_cast<int16_t>(leftStick.dx));
        gamepadState.set_LeftStickY(static_cast<int16_t>(leftStick.dy));

        (*gamepads)[0] = gamepadState;
        frameState.set_Gamepads(gamepads);
        return frameState;
    }
}
#endif
