using UnityEngine;
using UnityEngine.InputSystem;

namespace DragonQuest.Input
{
    // Read devices on demand so connecting/reconnecting never requires restarting the game.
    public static class GameInput
    {
        private static readonly ControllerNavigation repeat = new ControllerNavigation();
        private static int navigationFrame = -1, consumedFrame = -1;
        private static MenuDirection navigation;
        private static int observedDevice = -1;
        private static System.Func<bool> hasFocus = () => Application.isFocused;
        // One gate for exploration and every screen, also replaceable in isolated input checks.
        public static bool IsFocused => hasFocus();
        public static bool IsFrameAvailable => IsFocused && consumedFrame != Time.frameCount;
        private static bool PadAvailable => IsFrameAvailable
            && Gamepad.current != null && Gamepad.current.enabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            repeat.Reset(); navigationFrame = consumedFrame = observedDevice = -1; navigation = MenuDirection.None;
            hasFocus = () => Application.isFocused;
        }

        public static Vector2 Movement
        {
            get
            {
                if (!IsFocused) return Vector2.zero;
                Keyboard keys = Keyboard.current;
                Vector2 keyboard = Vector2.zero;
                if (keys != null)
                {
                    if (keys.aKey.isPressed || keys.leftArrowKey.isPressed) keyboard.x--;
                    if (keys.dKey.isPressed || keys.rightArrowKey.isPressed) keyboard.x++;
                    if (keys.sKey.isPressed || keys.downArrowKey.isPressed) keyboard.y--;
                    if (keys.wKey.isPressed || keys.upArrowKey.isPressed) keyboard.y++;
                }
                Vector2 pad = PadMovement;
                return Vector2.ClampMagnitude(keyboard.sqrMagnitude >= pad.sqrMagnitude ? keyboard : pad, 1);
            }
        }
        private static Vector2 PadMovement
        {
            get
            {
                if (!PadAvailable) return Vector2.zero;
                // StickControl already applies the Input System's radial dead zone.
                Vector2 stick = Gamepad.current.leftStick.ReadValue(), dpad = Gamepad.current.dpad.ReadValue();
                return dpad.sqrMagnitude > stick.sqrMagnitude ? dpad : stick;
            }
        }
        public static MenuDirection Navigation
        {
            get
            {
                if (!PadAvailable) { repeat.Reset(); observedDevice = -1; return MenuDirection.None; }
                if (observedDevice != Gamepad.current.deviceId)
                { repeat.Reset(); navigationFrame = -1; observedDevice = Gamepad.current.deviceId; }
                if (navigationFrame != Time.frameCount)
                {
                    Vector2 value = PadMovement;
                    navigation = repeat.Step(value.x, value.y, Time.unscaledTimeAsDouble);
                    navigationFrame = Time.frameCount;
                }
                return navigation;
            }
        }
        public static bool GamepadConfirmPressed => PadAvailable && Gamepad.current.buttonSouth.wasPressedThisFrame;
        public static bool GamepadCancelPressed => PadAvailable && Gamepad.current.buttonEast.wasPressedThisFrame;
        public static bool ConfirmPressed => IsFrameAvailable && (GamepadConfirmPressed
            || Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame
                || Keyboard.current.numpadEnterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame
                || Keyboard.current.eKey.wasPressedThisFrame));
        public static bool CancelPressed => IsFrameAvailable && (GamepadCancelPressed
            || Keyboard.current?.escapeKey.wasPressedThisFrame == true);
        public static bool InteractPressed => IsFrameAvailable && (GamepadConfirmPressed
            || Keyboard.current?.eKey.wasPressedThisFrame == true);
        public static bool InventoryPressed => IsFrameAvailable && (PadAvailable
            && (Gamepad.current.buttonNorth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame)
            || Keyboard.current?.iKey.wasPressedThisFrame == true);
        public static bool SavePressed => IsFrameAvailable && (PadAvailable && Gamepad.current.selectButton.wasPressedThisFrame
            || Keyboard.current?.f5Key.wasPressedThisFrame == true);
        public static bool CheatsPressed => IsFrameAvailable && (PadAvailable && Gamepad.current.rightStickButton.wasPressedThisFrame
            || Keyboard.current?.backquoteKey.wasPressedThisFrame == true);

        // Legacy name: consume keyboard and gamepad presses so one screen cannot activate another.
        public static void ConsumeGamepadFrame() => consumedFrame = Time.frameCount;
    }
}
