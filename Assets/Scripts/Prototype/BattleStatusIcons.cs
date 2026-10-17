using System.Collections.Generic;
using DragonQuest.Combat;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Small pixel glyphs match the existing GUI and remain legible at battle scale.
    public sealed class BattleStatusIcons
    {
        private readonly Dictionary<CombatStatus, Texture2D> textures = new Dictionary<CombatStatus, Texture2D>();
        private GUIStyle count;
        private static string[] Pattern(CombatStatus status)
        {
            switch (status)
            {
                case CombatStatus.Poison: return new[] { "   ##   ", "   ##   ", "  ####  ", " ###### ", " ##  ## ", " ###### ", "  ####  ", "   ##   " };
                case CombatStatus.Shield: return new[] { " ###### ", "########", "########", "########", " ###### ", "  ####  ", "   ##   ", "        " };
                case CombatStatus.Defending: return new[] { " ###### ", " ##  ## ", " ##  ## ", " ##  ## ", " ##  ## ", "  ####  ", "   ##   ", "        " };
                case CombatStatus.Protected: return new[] { " ##  ## ", "########", "########", " ###### ", "  ####  ", "   ##   ", "        ", "        " };
                case CombatStatus.Taunted: return new[] { "   ##   ", " ###### ", "## ## ##", "########", "## ## ##", " ###### ", "   ##   ", "        " };
                case CombatStatus.Bound: return new[] { " ###    ", "## ##   ", "## ##   ", " ###    ", "    ### ", "   ## ##", "   ## ##", "    ### " };
                case CombatStatus.Charging: return new[] { "    ##  ", "   ##   ", "  ##### ", " ####   ", "   ##   ", "  ##    ", " ##     ", "        " };
                default: return new[] { "##    ##", " ##  ## ", "  ####  ", "   ##   ", "  ####  ", " ##  ## ", "##    ##", "        " };
            }
        }
        private Texture2D Icon(CombatStatus status)
        {
            if (textures.TryGetValue(status, out Texture2D cached)) return cached;
            var texture = new Texture2D(8, 8) { filterMode = FilterMode.Point };
            Color color = status == CombatStatus.Poison ? BattleStatusColors.Poison
                : status == CombatStatus.Shield ? new Color(.25f, .9f, 1)
                : status == CombatStatus.Defeated ? new Color(1, .3f, .25f)
                : status == CombatStatus.Bound ? new Color(.8f, .8f, .85f) : new Color(1, .85f, .35f);
            string[] pattern = Pattern(status);
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
                texture.SetPixel(x, 7-y, pattern[y][x] == '#' ? color : Color.clear);
            texture.Apply(); textures[status] = texture;
            return texture;
        }
        public void Draw(float x, float y, CombatantState member)
            => Draw(x, y, CombatStatuses.Active(member));
        public void Draw(float x, float y, IReadOnlyList<StatusIndicator> statuses)
        {
            if (count == null)
            {
                count = PrototypeTypography.Style(GUI.skin.label, 12, true);
                count.alignment = TextAnchor.LowerRight; count.padding = new RectOffset(0,0,0,0);
            }
            int index = 0;
            foreach (StatusIndicator indicator in statuses)
            {
                Rect rect = new Rect(x + index % 4 * 18, y + index / 4 * 18, 16, 16); index++;
                string tooltip = indicator.Description + (indicator.Turns > 0 ? ": " + indicator.Turns + " turno(s) restante(s)" : "");
                GUI.DrawTexture(rect, Icon(indicator.Status), ScaleMode.StretchToFill, true);
                GUI.Label(rect, new GUIContent("", tooltip), GUIStyle.none);
                if (indicator.Turns > 0) GUI.Label(new Rect(rect.x+8,rect.y+6,10,12), indicator.Turns.ToString(), count);
            }
        }
    }
}
