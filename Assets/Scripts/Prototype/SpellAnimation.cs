using System;
using System.Collections.Generic;
using DragonQuest.Combat;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class SpellAnimation
    {
        public const float Duration = 1.0f;
        public const float ImpactDelay = 0.5f;
        private sealed class Sheet
        {
            public Texture2D Texture;
            public Rect[] Frames = new Rect[8];
            public Vector2[] Size = new Vector2[8];
            public int PeakHeight;
        }
        private readonly Dictionary<string, Sheet> sheets = new Dictionary<string, Sheet>();

        public static string Resource(AbilityDefinition ability)
        {
            if (ability == null || ability.MenuGroup == AbilityMenuGroup.Summon) return null;
            switch (ability.SourceAbilityId)
            {
                case "fire": return "FireSpell";
                case "ice": return "IceSpell";
                case "thunder": return "LightningSpell";
                case "earth": return "EarthSpell";
                case "chaos": return "ChaosSpell";
                case "heal": return "HealSpell";
                case "cleanse": return "CleanseSpell";
                case "revive": return "ReviveSpell";
                case "attack":
                    switch (ability.Element)
                    {
                        case DamageElement.Fire: return "FireSpell";
                        case DamageElement.Ice: return "IceSpell";
                        case DamageElement.Lightning: return "LightningSpell";
                        case DamageElement.Earth: return "EarthSpell";
                        case DamageElement.Chaos: return "ChaosSpell";
                    }
                    break;
            }
            return null;
        }

        public static int Frame(float elapsed) => Math.Max(0, Math.Min((int)(elapsed / Duration * 8), 7));

        private Sheet Load(string resource)
        {
            if (sheets.TryGetValue(resource, out Sheet cached)) return cached;
            Texture2D texture = Resources.Load<Texture2D>("PixelArt/Spells/" + resource);
            if (texture == null) { sheets[resource] = null; return null; }
            texture.filterMode = FilterMode.Point;
            var sheet = new Sheet { Texture = texture };
            // Generated sheets have uneven gutters. Locate low-alpha column valleys,
            // then trim each frame without changing its pixels or transparency.
            Color32[] pixels = texture.GetPixels32();
            for (int row = 0; row < 2; row++)
            {
                int rowBottom = row == 0 ? texture.height / 2 : 0;
                int rowTop = row == 0 ? texture.height : texture.height / 2;
                int[] density = new int[texture.width];
                for (int y = rowBottom; y < rowTop; y++)
                    for (int x = 0; x < texture.width; x++)
                        if (pixels[y * texture.width + x].a >= 128) density[x]++;
                int[] boundaries = { 0, 0, 0, 0, texture.width };
                for (int col = 1; col < 4; col++)
                {
                    int expected = texture.width * col / 4;
                    int radius = texture.width / 9;
                    int best = expected;
                    float bestScore = float.MaxValue;
                    for (int x = expected - radius; x <= expected + radius; x++)
                    {
                        float score = density[x] + Math.Abs(x - expected) * .03f;
                        if (score < bestScore) { bestScore = score; best = x; }
                    }
                    boundaries[col] = best;
                }
                for (int col = 0; col < 4; col++)
                {
                    int left = boundaries[col + 1], right = boundaries[col] - 1;
                    int bottom = rowTop, top = rowBottom - 1;
                    for (int y = rowBottom; y < rowTop; y++)
                        for (int x = boundaries[col]; x < boundaries[col + 1]; x++)
                            if (pixels[y * texture.width + x].a >= 128)
                            {
                                left = Math.Min(left, x); right = Math.Max(right, x);
                                bottom = Math.Min(bottom, y); top = Math.Max(top, y);
                            }
                    if (right < left) continue;
                    int frame = row * 4 + col;
                    sheet.Size[frame] = new Vector2(right - left + 1, top - bottom + 1);
                    sheet.PeakHeight = Math.Max(sheet.PeakHeight, top - bottom + 1);
                    sheet.Frames[frame] = new Rect((float)left / texture.width, (float)bottom / texture.height,
                        sheet.Size[frame].x / texture.width, sheet.Size[frame].y / texture.height);
                }
            }
            sheets[resource] = sheet;
            return sheet;
        }

        public bool Draw(Rect target, AbilityDefinition ability, float elapsed)
        {
            string resource = Resource(ability);
            if (resource == null || elapsed < 0 || elapsed >= Duration) return false;
            Sheet sheet = Load(resource);
            if (sheet == null) return false;
            float height = Mathf.Clamp(target.height * 1.6f, 130, 260);
            int frame = Frame(elapsed);
            if (sheet.PeakHeight == 0) return false;
            Vector2 size = sheet.Size[frame] * (height / sheet.PeakHeight);
            Rect destination = new Rect(target.center.x - size.x / 2, target.yMax - size.y + 12, size.x, size.y);
            Color previous = GUI.color; GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(destination, sheet.Texture, sheet.Frames[frame], true);
            GUI.color = previous;
            return true;
        }
    }
}
