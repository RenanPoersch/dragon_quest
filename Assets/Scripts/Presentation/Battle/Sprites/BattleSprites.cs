using DragonQuest.Presentation.Battle.Animations;
using System;
using System.Collections.Generic;
using UnityEngine;
using DragonQuest.Combat;

namespace DragonQuest.Presentation.Battle.Sprites
{
    // Dedicated battle sheets: idle / wind-up / impact / recovery, all facing the enemy side.
    public sealed class BattleSprites
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
        public static bool HasAnimation(string id, AbilityDefinition ability) => ability != null
            && BattleAnimationCatalog.Resource(id, ability) != null;
        public BattleSprites()
        {
            // Original generated sheets have unequal gutters around extended weapons/effects.
            Load("aren/attack", resources["aren/attack"], new[] { 0, 512, 918, 1711, 2172 });
            Load("lia/attack", resources["lia/attack"], new[] { 0, 492, 1038, 1665, 2172 });
            Load("bram/attack", resources["bram/attack"], new[] { 0, 554, 1011, 1687, 2172 });
            foreach (string id in new[] { "guard", "pirate", "golem", "wolf", "tyrant", "evil-spirit" })
                Load(id + "/attack", BattleAnimationCatalog.Idle(id), null);
            foreach (string resource in new[] { "TyrantRoyalStrike", "TyrantRoyalHeavy", "TyrantMeteorCharge", "TyrantFlamingMeteor" })
                Load(resource, resource, null);
        }

        private void Load(string id, string resource, int[] boundaries)
        {
            Texture2D texture = Resources.Load<Texture2D>("PixelArt/" + resource);
            if (texture == null) return;
            Color32[] pixels = texture.GetPixels32();
            // The selected spirit is one complete sprite, shared by all action phases.
            bool singlePose = resource == "SpiritBattle";
            if (boundaries == null && !singlePose)
            {
                var density = new int[texture.width];
                for (int y = 0; y < texture.height; y++) for (int x = 0; x < texture.width; x++)
                    if (pixels[y * texture.width + x].a >= 128) density[x]++;
                boundaries = new[] { 0, 0, 0, 0, 2172 };
                for (int col = 1; col < 4; col++)
                {
                    int expected = texture.width * col / 4, radius = texture.width / 12, best = expected;
                    float score = float.MaxValue;
                    for (int x = expected - radius; x <= expected + radius; x++)
                    {
                        float candidate = density[x] + Math.Abs(x - expected) * .03f;
                        if (candidate < score) { score = candidate; best = x; }
                    }
                    boundaries[col] = (int)Math.Round(best * 2172f / texture.width);
                }
            }
            var sheet = new Sheet { Texture = texture };
            for (int frame = 0; frame < 4; frame++)
            {
                int cellLeft = singlePose ? 0 : boundaries[frame] * texture.width / 2172;
                int cellRight = singlePose ? texture.width : boundaries[frame + 1] * texture.width / 2172;
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
            float referenceBottom = sheet.Uv[0].y * texture.height;
            for (int frame = 0; frame < 4; frame++)
                sheet.Foot[frame].y = sheet.Uv[frame].y * texture.height - referenceBottom;
            sheets.Add(id, sheet);
        }

        public bool Draw(Rect area, string id, int frame, Color tint, AbilityDefinition ability = null)
        {
            string family = BattleAnimationCatalog.Family(id);
            string resource = BattleAnimationCatalog.Resource(id, ability);
            string key = BattleAnimationCatalog.Sequential(resource) ? resource : family + "/attack";
            if (ability != null && resource == null) frame = 0;
            if (!sheets.TryGetValue(key, out Sheet sheet)) return false;
            frame = Mathf.Clamp(frame, 0, 3);
            // All poses use idle body scale, rather than shrinking the character to fit attack effects.
            float bodyHeight = sheet.BodyHeight;
            // The raised mace belongs to anticipation, not to the king's body height.
            if (resource == "TyrantRoyalStrike" || resource == "TyrantRoyalHeavy") bodyHeight *= .8f;
            float scale = area.height / bodyHeight;
            Vector2 size = sheet.Size[frame] * scale;
            Rect destination = new Rect(area.center.x - sheet.Foot[frame].x * scale,
                area.yMax - size.y - sheet.Foot[frame].y * scale, size.x, size.y);
            Color previous = GUI.color; GUI.color = tint;
            GUI.DrawTextureWithTexCoords(destination, sheet.Texture, sheet.Uv[frame], true);
            GUI.color = previous;
            return true;
        }

        public bool DrawMeteor(Rect area, float elapsed, Color tint)
        {
            if (!sheets.TryGetValue("TyrantFlamingMeteor", out Sheet sheet)) return false;
            int frame = BattleAnimationCatalog.Frame("TyrantFlamingMeteor", elapsed, 1.6f);
            float peakHeight = 0;
            foreach (Vector2 size in sheet.Size) peakHeight = Mathf.Max(peakHeight, size.y);
            float scale = area.height / peakHeight;
            Vector2 dimensions = sheet.Size[frame] * scale;
            Rect destination = new Rect(area.center.x - dimensions.x * .5f, area.yMax - dimensions.y, dimensions.x, dimensions.y);
            Color previous = GUI.color; GUI.color = tint;
            GUI.DrawTextureWithTexCoords(destination, sheet.Texture, sheet.Uv[frame], true);
            GUI.color = previous;
            return true;
        }
    }
}
