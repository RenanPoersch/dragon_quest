using System.Collections.Generic;
using DragonQuest.Input;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // IMGUI has no gamepad focus. Keep its existing buttons, mouse input and scroll views.
    public sealed class ControllerMenu
    {
        private readonly ControllerMenuFocus focus = new ControllerMenuFocus();
        private readonly List<bool> buttons = new List<bool>();
        private int buttonIndex, scrollIndex, currentScroll = -1, selectedScroll = -1;
        private Rect selectedRect;
        private int selectedRectIndex = -1, revealIndex = -1;
        private int processedFrame = -1, lastDrawFrame = -10, activatedFrame = -1;
        private bool confirm;

        public void Begin(bool blocked)
        {
            if (Event.current.type == EventType.KeyDown) Event.current.Use();
            buttonIndex = scrollIndex = 0; currentScroll = -1;
            if (Event.current.type == EventType.Repaint) buttons.Clear();
            if (processedFrame == Time.frameCount) return;
            bool opened = Time.frameCount > lastDrawFrame + 1;
            if (opened) { focus.Reset(); selectedScroll = revealIndex = -1; }
            processedFrame = lastDrawFrame = Time.frameCount;
            confirm = !blocked && !opened && GameInput.ConfirmPressed;
            if (blocked) return;
            MenuDirection direction = GameInput.Navigation;
            if (direction != MenuDirection.None)
            {
                focus.Move(direction == MenuDirection.Up || direction == MenuDirection.Left ? -1 : 1);
                revealIndex = focus.Selected;
            }
        }
        public void End()
        {
            if (Event.current.type == EventType.Repaint) focus.SetButtons(buttons);
        }
        public bool Button(Rect area, string label, GUIStyle style)
        {
            int index = buttonIndex++;
            bool selected = index == focus.Selected;
            if (Event.current.type == EventType.Repaint)
            {
                buttons.Add(GUI.enabled);
                if (selected)
                {
                    selectedRect = area; selectedRectIndex = index; selectedScroll = currentScroll;
                    if (currentScroll < 0) revealIndex = -1;
                }
            }
            Color previous = GUI.backgroundColor;
            if (selected && GUI.enabled) GUI.backgroundColor = new Color(1, .8f, .3f);
            bool clicked = GUI.Button(area, label, style);
            GUI.backgroundColor = previous;
            if (selected && GUI.enabled && Event.current.type == EventType.Repaint)
            {
                Color color = GUI.color; GUI.color = new Color(1, .85f, .35f);
                GUI.DrawTexture(new Rect(area.x, area.y, area.width, 2), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(area.x, area.yMax - 2, area.width, 2), Texture2D.whiteTexture);
                GUI.color = color;
            }
            bool activate = selected && confirm && GUI.enabled && activatedFrame != Time.frameCount
                && Event.current.type == EventType.Repaint;
            if (activate || clicked)
            { activatedFrame = Time.frameCount; confirm = false; GameInput.ConsumeGamepadFrame(); }
            return clicked || activate;
        }
        public Vector2 BeginScrollView(Rect viewport, Vector2 scroll, Rect content)
        {
            currentScroll = scrollIndex++;
            if (selectedScroll == currentScroll && revealIndex == focus.Selected && selectedRectIndex == focus.Selected)
            {
                if (selectedRect.y < scroll.y) scroll.y = selectedRect.y;
                if (selectedRect.yMax > scroll.y + viewport.height) scroll.y = selectedRect.yMax - viewport.height;
                scroll.y = Mathf.Clamp(scroll.y, 0, Mathf.Max(0, content.height - viewport.height));
                revealIndex = -1;
            }
            return GUI.BeginScrollView(viewport, scroll, content);
        }
        public void EndScrollView() { GUI.EndScrollView(); currentScroll = -1; }
    }
}
