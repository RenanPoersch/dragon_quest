using DragonQuest.Presentation.Common;
using System.Collections.Generic;
using DragonQuest.Combat;
using UnityEngine;

namespace DragonQuest.Presentation.Battle
{
    // Small pixel glyphs match the existing GUI and remain legible at battle scale.
    public sealed class BattleStatusIcons : System.IDisposable
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
                case CombatStatus.Taunted: return new[]
                {
                    "##            ##",
                    "#B##        ##B#",
                    " #BB##    ##BB# ",
                    " #WBB##  ##BBW# ",
                    " #WWPW#  #WPWW# ",
                    "  #WPW#  #WPW#  ",
                    "   ###    ###   ",
                    "                "
                };
                case CombatStatus.Bound: return new[] { " ###    ", "## ##   ", "## ##   ", " ###    ", "    ### ", "   ## ##", "   ## ##", "    ### " };
                case CombatStatus.MindControlled: return new[] { "  ####  ", " ##  ## ", "     ## ", "   ###  ", "   ##   ", "        ", "   ##   ", "        " };
                case CombatStatus.Silenced: return new[] { "        ", "        ", "        ", "        ", "## ## ##", "## ## ##", "        ", "        " };
                case CombatStatus.Charging: return new[] { "    ##  ", "   ##   ", "  ##### ", " ####   ", "   ##   ", "  ##    ", " ##     ", "        " };
                default: return new[] { "##    ##", " ##  ## ", "  ####  ", "   ##   ", "  ####  ", " ##  ## ", "##    ##", "        " };
            }
        }
        private Texture2D Icon(CombatStatus status)
        {
            if (textures.TryGetValue(status, out Texture2D cached)) return cached;
            Color color = status == CombatStatus.Poison ? BattleStatusColors.Poison
                : status == CombatStatus.Shield ? new Color(.25f, .9f, 1)
                : status == CombatStatus.MindControlled ? new Color(.85f, .45f, 1f)
                : status == CombatStatus.Silenced ? new Color(1f, .85f, .25f)
                : status == CombatStatus.Defeated ? new Color(1, .3f, .25f)
                : status == CombatStatus.Bound ? new Color(.8f, .8f, .85f) : new Color(1, .85f, .35f);
            string[] pattern = Pattern(status);
            int width = pattern[0].Length, height = pattern.Length;
            var texture = new Texture2D(width, height) { filterMode = FilterMode.Point };
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                char pixel = pattern[y][x];
                Color ink = status == CombatStatus.Taunted
                    ? pixel == 'B' ? new Color(1f, .78f, .25f)
                    : pixel == 'W' ? new Color(1f, .97f, .85f) : new Color(.08f, .06f, .16f)
                    : color;
                texture.SetPixel(x, height - 1 - y, pixel == ' ' ? Color.clear : ink);
            }
            texture.Apply(); textures[status] = texture;
            return texture;
        }
        public void DrawOverhead(Rect figure, bool confused, bool silenced, float time, bool taunted = false)
        {
            if (confused)
                for (int i = 0; i < 3; i++)
                {
                    float angle = time * 3.5f + i * Mathf.PI * 2 / 3;
                    float x = figure.center.x + Mathf.Cos(angle) * 25;
                    float y = figure.y - 18 + Mathf.Sin(angle) * 7;
                    GUI.DrawTexture(new Rect(x - 8, y - 12, 16, 16), Icon(CombatStatus.MindControlled), ScaleMode.StretchToFill, true);
                }
            if (silenced)
                GUI.DrawTexture(new Rect(figure.center.x - 18, figure.y - (confused ? 56 : 30), 36, 24),
                    Icon(CombatStatus.Silenced), ScaleMode.StretchToFill, true);
            if (taunted)
            {
                // Keep the eyes above the turn arrow and any confusion/silence glyphs.
                float offset = confused && silenced ? 86 : confused || silenced ? 68 : 60;
                GUI.DrawTexture(new Rect(figure.center.x - 24, figure.y - offset, 48, 24),
                    Icon(CombatStatus.Taunted), ScaleMode.ScaleToFit, true);
            }
        }

        public void Draw(float x, float y, CombatantState member)
            => Draw(x, y, CombatStatuses.Active(member));
        public void Draw(float x, float y, IReadOnlyList<StatusIndicator> statuses)
        {
            if (count == null)
            {
                count = GameTypography.Style(GUI.skin.label, 12, true);
                count.alignment = TextAnchor.LowerRight; count.padding = new RectOffset(0,0,0,0);
            }
            int index = 0;
            foreach (StatusIndicator indicator in statuses)
            {
                Rect rect = new Rect(x + index % 4 * 18, y + index / 4 * 18, 16, 16); index++;
                GUI.DrawTexture(rect, Icon(indicator.Status), ScaleMode.ScaleToFit, true);
                if (indicator.Turns > 0) GUI.Label(new Rect(rect.x+8,rect.y+6,10,12), indicator.Turns.ToString(), count);
            }
        }

        public void Dispose()
        {
            foreach (Texture2D texture in textures.Values)
                if (texture != null) UnityEngine.Object.Destroy(texture);
            textures.Clear();
        }
    }
}
