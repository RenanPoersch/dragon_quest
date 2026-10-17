using System;
using DragonQuest.Input;

public static class ControllerChecks
{
    private static int checks;
    private static void Check(bool value, string message)
    { checks++; if (!value) throw new Exception(message); }
    public static int Main()
    {
        var repeat = new ControllerNavigation();
        Check(repeat.Step(.2f, -.2f, 0) == MenuDirection.None, "Stick drift must not move a menu.");
        Check(repeat.Step(0, -1, .1) == MenuDirection.Down, "A directional press moves immediately.");
        Check(repeat.Step(0, -1, .2) == MenuDirection.None, "Holding cannot skip several options immediately.");
        Check(repeat.Step(0, -.5f, .3) == MenuDirection.None, "Hysteresis tolerates noise near the press threshold.");
        Check(repeat.Step(0, -1, .501) == MenuDirection.Down, "A held direction repeats after the initial delay.");
        Check(repeat.Step(0, -1, .55) == MenuDirection.None, "Repeat is rate limited.");
        Check(repeat.Step(0, -1, .622) == MenuDirection.Down, "Repeat continues at its repeat interval.");
        Check(repeat.Step(0, 1, .63) == MenuDirection.Up, "Reversing direction responds immediately.");
        Check(repeat.Step(1, .7f, .64) == MenuDirection.Right, "Only the dominant diagonal axis moves focus.");
        Check(repeat.Step(-1, .5f, .65) == MenuDirection.Left, "All four menu directions are supported.");
        Check(repeat.Step(0, 0, .66) == MenuDirection.None, "Releasing resets repeat.");
        Check(repeat.Step(0, -1, .67) == MenuDirection.Down, "A fresh press does not inherit the previous delay.");
        repeat.Reset();
        Check(repeat.Step(0, -1, .68) == MenuDirection.Down, "Reconnection/focus reset starts a fresh press.");
        Check(repeat.Step(0, -1, 20) == MenuDirection.Down, "A slow frame emits one repeat.");
        Check(repeat.Step(0, -1, 20) == MenuDirection.None, "A slow frame does not accumulate extra navigation.");
        var menu = new ControllerMenuFocus();
        menu.Move(1);
        Check(menu.Selected == -1, "Empty menus are safe.");
        menu.SetButtons(new[] { false, true, false, true });
        Check(menu.Selected == 1, "Disabled entries are skipped on opening.");
        menu.Move(1); Check(menu.Selected == 3, "Forward navigation skips disabled entries.");
        menu.Move(1); Check(menu.Selected == 1, "Navigation wraps from the last enabled button.");
        menu.Move(-1); Check(menu.Selected == 3, "Reverse navigation wraps and skips disabled entries.");
        menu.SetButtons(new[] { true });
        Check(menu.Selected == 0, "Changing a tab with fewer buttons restores valid focus.");
        menu.SetButtons(new[] { false, false });
        Check(menu.Selected == -1, "A menu with no enabled buttons has no activation target.");
        menu.SetButtons(new[] { true, true });
        Check(menu.Selected == 0, "Newly enabled entries can be reached.");
        menu.Reset(); Check(menu.Selected == -1, "Reopening discards stale selection.");
        // Exercise every control in a large, scrolling inventory in both directions.
        var enabled = new bool[80];
        for (int i = 0; i < enabled.Length; i++) enabled[i] = i % 5 != 0;
        menu.SetButtons(enabled);
        for (int i = 0; i < 128; i++)
        {
            int old = menu.Selected;
            menu.Move(1); Check(menu.Selected != old && enabled[menu.Selected], "Large inventories keep a valid, reachable selection.");
            menu.Move(-1); Check(menu.Selected == old, "Reverse movement returns to the same entry.");
            menu.Move(1);
        }
        Console.WriteLine("CONTROLLER_CHECKS_OK: " + checks + " navigation, repeat, drift, disabled buttons and inventory checks.");
        return 0;
    }
}
