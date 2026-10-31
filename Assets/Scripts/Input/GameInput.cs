using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DragonQuest.Input
{
    // A screen reads once before handling its commands; consuming then blocks every later reader.
    public readonly struct MenuInput
    {
        public bool Available { get; }
        public MenuDirection Direction { get; }
        public bool Confirm { get; }
        public bool Cancel { get; }
        public int Horizontal => Direction == MenuDirection.Left ? -1 : Direction == MenuDirection.Right ? 1 : 0;
        public int Vertical => Direction == MenuDirection.Up ? -1 : Direction == MenuDirection.Down ? 1 : 0;
        public int Step => Horizontal != 0 ? Horizontal : Vertical;

        internal MenuInput(MenuDirection direction, bool confirm, bool cancel)
        { Available = true; Direction = direction; Confirm = confirm; Cancel = cancel; }
    }

    // Read devices on demand so connecting/reconnecting never requires restarting the game.
    public static class GameInput
    {
        private static readonly ControllerNavigation repeat = new ControllerNavigation();
        private static int navigationFrame = -1, consumedFrame = -1;
        private static MenuDirection navigation;
        private static uint navigationUpdate;
        private static int observedDevice = -1;
        // One focus gate for exploration and every screen.
        public static bool IsFocused => Application.isFocused;
        public static bool IsFrameAvailable => IsFocused && consumedFrame != Time.frameCount;
        // Continuous scrolling remains available after the dialogue handles its discrete commands.
        public static float ScrollAxis => IsFocused && Gamepad.current != null
            ? Gamepad.current.rightStick.ReadValue().y : 0;
        private static bool PadAvailable => IsFrameAvailable
            && Gamepad.current != null && Gamepad.current.enabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            repeat.Reset(); navigationFrame = consumedFrame = observedDevice = -1; navigation = MenuDirection.None;
            navigationUpdate = 0;
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
                if (!IsFrameAvailable)
                {
                    // Consuming a screen's input must not restart the held-direction repeat.
                    if (!IsFocused) { repeat.Reset(); observedDevice = navigationFrame = -1; }
                    return MenuDirection.None;
                }
                int device = Gamepad.current?.deviceId ?? -1;
                if (observedDevice != device)
                { repeat.Reset(); navigationFrame = -1; observedDevice = device; }
                uint update = InputState.updateCount;
                if (navigationFrame != Time.frameCount || navigationUpdate != update)
                {
                    // Keyboard, D-pad and stick share direction, delay and repeat in every menu.
                    Vector2 value = Movement;
                    navigation = repeat.Step(value.x, value.y, Time.unscaledTimeAsDouble);
                    navigationFrame = Time.frameCount; navigationUpdate = update;
                }
                return navigation;
            }
        }
        public static bool EscapeHeld => IsFocused && (Keyboard.current != null
            && Keyboard.current.qKey.isPressed && Keyboard.current.eKey.isPressed
            || PadAvailable && Gamepad.current.leftTrigger.isPressed && Gamepad.current.rightTrigger.isPressed);
        public static bool KeyPressed(Key key) => IsFrameAvailable && Keyboard.current != null
            && Keyboard.current[key].wasPressedThisFrame;
        public static bool GamepadButtonPressed(GamepadButton button) => PadAvailable
            && Gamepad.current[button].wasPressedThisFrame;
        public static bool GamepadConfirmPressed => GamepadButtonPressed(GamepadButton.South);
        public static bool GamepadCancelPressed => GamepadButtonPressed(GamepadButton.East);
        public static bool ConfirmPressed => GamepadConfirmPressed || KeyPressed(Key.Enter)
            || KeyPressed(Key.NumpadEnter) || KeyPressed(Key.Space) || KeyPressed(Key.E);
        public static bool CancelPressed => GamepadCancelPressed || KeyPressed(Key.Escape);
        public static bool InteractPressed => GamepadConfirmPressed || KeyPressed(Key.E);
        public static bool InventoryPressed => GamepadButtonPressed(GamepadButton.North)
            || GamepadButtonPressed(GamepadButton.Start) || KeyPressed(Key.I);
        public static bool SavePressed => KeyPressed(Key.F5);
        public static bool CheatsPressed => GamepadButtonPressed(GamepadButton.Select)
            || GamepadButtonPressed(GamepadButton.RightStick) || KeyPressed(Key.Backquote);

        public static MenuInput ReadMenu() => IsFrameAvailable
            ? new MenuInput(Navigation, ConfirmPressed, CancelPressed) : default;

        public static void ConsumeFrame() => consumedFrame = Time.frameCount;
    }
}
