using DragonQuest.Combat;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Five rows, four poses per action. Generated pixels and alpha remain untouched.
    public sealed class SpiritAttackAnimation
    {
        private BattleAnimationSheet sheet;
        private Texture2D mote;
        public void Prepare()
        {
            if (sheet == null) sheet = new BattleAnimationSheet("PixelArt/SpiritAttacks", 4, 5, true);
            if (mote != null) return;
            mote = new Texture2D(5, 5) { filterMode = FilterMode.Point };
            for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++)
                mote.SetPixel(x, y, Mathf.Abs(x - 2) + Mathf.Abs(y - 2) <= 2 ? Color.white : Color.clear);
            mote.Apply();
        }
        public static int Row(AbilityDefinition ability)
        {
            switch (ability?.SourceAbilityId)
            {
                case "mind-control": return 0;
                case "silence": return 1;
                case "drain-life": return 2;
                case "chains": return 3;
                case "chaos-magic": return 4;
                default: return -1;
            }
        }
        public bool Draw(Rect figure, AbilityDefinition ability, float elapsed, Color tint)
        {
            int row = Row(ability);
            if (row < 0) return false;
            Prepare();
            int frame = Mathf.Clamp((int)(elapsed / 1.4f * 4), 0, 3);
            return sheet.Draw(new Vector2(figure.center.x, figure.yMax), figure.height, row * 4 + frame, tint);
        }
        public void DrawEffect(Rect caster, Rect target, AbilityDefinition ability, float elapsed)
        {
            int row = Row(ability);
            if (row < 0 || elapsed < .35f || elapsed > 1.35f) return;
            Prepare();
            bool drain = row == 2;
            Vector2 from = drain ? target.center : caster.center;
            Vector2 to = drain ? caster.center : target.center;
            Color previous = GUI.color;
            Color color = row == 0 || row == 4 ? new Color(.8f, .3f, 1)
                : row == 1 ? new Color(1, .85f, .25f) : drain ? new Color(.3f, 1, .4f) : new Color(.5f, .8f, 1);
            color.a = Mathf.Clamp01((1.4f - elapsed) * 5);
            GUI.color = color;
            for (int i = 0; i < 9; i++)
            {
                float progress = Mathf.Clamp01((elapsed - .35f) * 1.65f - i * .045f);
                Vector2 point = Vector2.Lerp(from, to, progress);
                point.y += Mathf.Sin(progress * Mathf.PI * 4 + i) * 12;
                GUI.DrawTexture(new Rect(point.x - 5, point.y - 5, 10, 10), mote, ScaleMode.StretchToFill, true);
            }
            GUI.color = previous;
        }
    }
}
