using System;
using System.Collections.Generic;

namespace DragonQuest.Input
{
    public enum MenuDirection { None, Up, Down, Left, Right }

    // Independent of frame rate. Hysteresis prevents stick noise from restarting the repeat.
    public sealed class ControllerNavigation
    {
        private MenuDirection held;
        private double nextRepeat;
        public void Reset() { held = MenuDirection.None; nextRepeat = 0; }
        public MenuDirection Step(float x, float y, double time)
        {
            float threshold = held == MenuDirection.None ? .55f : .35f;
            MenuDirection direction = Math.Abs(x) < threshold && Math.Abs(y) < threshold ? MenuDirection.None
                : Math.Abs(y) >= Math.Abs(x) ? (y > 0 ? MenuDirection.Up : MenuDirection.Down)
                : x > 0 ? MenuDirection.Right : MenuDirection.Left;
            if (direction == MenuDirection.None) { Reset(); return MenuDirection.None; }
            if (direction != held) { held = direction; nextRepeat = time + .4; return direction; }
            if (time < nextRepeat) return MenuDirection.None;
            nextRepeat = time + .12;
            return direction;
        }
    }

    public sealed class ControllerMenuFocus
    {
        private readonly List<bool> enabled = new List<bool>();
        public int Selected { get; private set; } = -1;
        public void Reset() { enabled.Clear(); Selected = -1; }
        public void SetButtons(IReadOnlyList<bool> buttons)
        {
            enabled.Clear();
            for (int i = 0; i < buttons.Count; i++) enabled.Add(buttons[i]);
            if (Selected < 0 || Selected >= enabled.Count || !enabled[Selected])
            { Selected = -1; Move(1); }
        }
        public void Move(int delta)
        {
            if (enabled.Count == 0 || delta == 0) return;
            int index = Selected < 0 ? (delta > 0 ? -1 : 0) : Selected;
            for (int i = 0; i < enabled.Count; i++)
            {
                index = (index + (delta > 0 ? 1 : -1) + enabled.Count) % enabled.Count;
                if (enabled[index]) { Selected = index; return; }
            }
            Selected = -1;
        }
    }
}
