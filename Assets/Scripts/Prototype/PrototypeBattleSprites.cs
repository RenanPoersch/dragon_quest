using System;
using System.Collections.Generic;
using UnityEngine;
using DragonQuest.Combat;

namespace DragonQuest.Prototype
{
    // Dedicated battle sheets: idle / wind-up / impact / recovery, all facing the enemy side.
    public sealed class PrototypeBattleSprites
    {
        private sealed class Sheet
        {
            public Texture2D Texture;
            public readonly Rect[] Uv = new Rect[4];
            public readonly Vector2[] Size = new Vector2[4];
            public readonly Vector2[] Foot = new Vector2[4];
            public float BodyHeight;
        }
        private readonly Dictionary<string, Sheet> sheets = new Dictionary<string, Sheet>();
        // Animation registrations are specific to hero + action, never to a broad damage effect.
        private static readonly Dictionary<string, string> resources = new Dictionary<string, string>
        {
            { "aren/attack", "ArenBattle" }, { "lia/attack", "LiaBattle" }, { "bram/attack", "BramBattle" }
        };
        private static string ActionKey(string id, AbilityDefinition ability) => id + "/" + (ability == null ? "attack" : ability.SourceAbilityId);
        public static bool HasAnimation(string id, AbilityDefinition ability) => ability != null
            && !ability.UsesLimit && resources.ContainsKey(ActionKey(id, ability));
        public PrototypeBattleSprites()
        {
            // Original generated sheets have unequal gutters around extended weapons/effects.
            Load("aren/attack", resources["aren/attack"], new[] { 0, 512, 918, 1711, 2172 });
            Load("lia/attack", resources["lia/attack"], new[] { 0, 492, 1038, 1665, 2172 });
            Load("bram/attack", resources["bram/attack"], new[] { 0, 554, 1011, 1687, 2172 });
        }

        private void Load(string id, string resource, int[] boundaries)
        {
            Texture2D texture = Resources.Load<Texture2D>("PixelArt/" + resource);
            if (texture == null) return;
            Color32[] pixels = texture.GetPixels32();
            var sheet = new Sheet { Texture = texture };
            for (int frame = 0; frame < 4; frame++)
            {
                int cellLeft = boundaries[frame] * texture.width / 2172;
                int cellRight = boundaries[frame + 1] * texture.width / 2172;
                int left = cellRight, right = cellLeft - 1, bottom = texture.height, top = -1;
                for (int y = 0; y < texture.height; y++) for (int x = cellLeft; x < cellRight; x++)
                {
                    if (pixels[y * texture.width + x].a < 128) continue;
                    left = Math.Min(left, x); right = Math.Max(right, x);
                    bottom = Math.Min(bottom, y); top = Math.Max(top, y);
                }
                if (right < left) { Debug.LogWarning("Pose de batalha vazia: " + resource + " / " + frame); return; }
                // Feet determine the anchor, so a sword or spell widening the silhouette does not move the hero.
                int footLeft = right, footRight = left;
                int footTop = Math.Min(top, bottom + Math.Max(2, (top - bottom) / 12));
                for (int y = bottom; y <= footTop; y++) for (int x = left; x <= right; x++)
                    if (pixels[y * texture.width + x].a >= 128)
                    { footLeft = Math.Min(footLeft, x); footRight = Math.Max(footRight, x); }
                sheet.Size[frame] = new Vector2(right - left + 1, top - bottom + 1);
                sheet.Foot[frame] = new Vector2((footLeft + footRight + 1) / 2f - left, 0);
                sheet.Uv[frame] = new Rect((float)left / texture.width, (float)bottom / texture.height,
                    sheet.Size[frame].x / texture.width, sheet.Size[frame].y / texture.height);
                if (frame == 0) sheet.BodyHeight = sheet.Size[frame].y;
            }
            sheets.Add(id, sheet);
        }

        public bool Draw(Rect area, string id, int frame, Color tint, AbilityDefinition ability = null)
        {
            if (!sheets.TryGetValue(ActionKey(id, ability), out Sheet sheet)) return false;
            frame = Mathf.Clamp(frame, 0, 3);
            // All poses use idle body scale, rather than shrinking the character to fit attack effects.
            float scale = area.height / sheet.BodyHeight;
            Vector2 size = sheet.Size[frame] * scale;
            Rect destination = new Rect(area.center.x - sheet.Foot[frame].x * scale,
                area.yMax - size.y, size.x, size.y);
            Color previous = GUI.color; GUI.color = tint;
            GUI.DrawTextureWithTexCoords(destination, sheet.Texture, sheet.Uv[frame], true);
            GUI.color = previous;
            return true;
        }
    }
}
