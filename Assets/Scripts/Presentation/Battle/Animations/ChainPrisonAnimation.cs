using DragonQuest.Infrastructure;
using System;
using UnityEngine;

namespace DragonQuest.Presentation.Battle.Animations
{
    // The prison is a persistent status overlay, independent of the boss's casting animation.
    public sealed class ChainPrisonAnimation
    {
        public const string Resource = "SpiritBoss/SpiritChainPrison";
        public const float BreakDuration = .6f;
        private Texture2D texture;
        private readonly Action<Texture2D, Rect, Rect, Color> draw;
        public ChainPrisonAnimation(Action<Texture2D, Rect, Rect, Color> draw = null)
        { this.draw = draw ?? DrawGui; }
        public Texture2D Prepare()
        {
            if (texture != null) return texture;
            texture = GameAssets.Required<Texture2D>("PixelArt/" + Resource);
            if (texture.width % 4 != 0 || texture.height % 2 != 0)
                throw new InvalidOperationException("Missing or invalid chain prison atlas.");
            texture.filterMode = FilterMode.Point;
            return texture;
        }
        private static void DrawGui(Texture2D source, Rect rect, Rect uv, Color tint)
        {
            Color before = GUI.color; GUI.color = tint;
            GUI.DrawTextureWithTexCoords(rect, source, uv, true);
            GUI.color = before;
        }
        public void Draw(Rect hero, bool bound, float time, float breakAge = -1)
        {
            bool breaking = breakAge >= 0 && breakAge < BreakDuration;
            if (!bound && !breaking) return;
            Prepare();
            int pose = breaking ? 4 + Mathf.Min(3, (int)(breakAge * 4 / BreakDuration))
                : (int)(Mathf.Max(0, time) * 5) % 4;
            float height = hero.height * .92f;
            float width = height * (texture.width / 4f) / (texture.height / 2f);
            Rect destination = new Rect(hero.center.x - width / 2, hero.y + hero.height * .60f - height / 2, width, height);
            Rect uv = new Rect((pose % 4) * .25f, pose < 4 ? .5f : 0, .25f, .5f);
            Color tint = Color.white;
            if (breaking) tint.a = 1 - Mathf.Clamp01((breakAge - .4f) / .2f);
            draw(texture, destination, uv, tint);
        }
    }
}
