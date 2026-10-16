using System;
using System.Collections.Generic;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Every conversation, including choices and narrated discoveries, uses this layout.
    public sealed class DialoguePresentation : IDisposable
    {
        private readonly Texture2D gradient;
        private readonly Dictionary<string, PixelSpriteAtlas> portraits = new Dictionary<string, PixelSpriteAtlas>();
        private GUIStyle title, body, choice, hint;
        private Vector2 scroll;
        private string lastText, lastTitle;

        public DialoguePresentation()
        {
            // Same palette and silver frame as the Load / New game panels.
            gradient = new Texture2D(128, 64, TextureFormat.RGB24, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < gradient.height; y++) for (int x = 0; x < gradient.width; x++)
            {
                float u = x / (float)(gradient.width - 1), v = y / (float)(gradient.height - 1);
                Color top = Color.Lerp(new Color(.025f, .23f, .62f), new Color(.015f, .015f, .27f), u);
                Color bottom = Color.Lerp(new Color(.01f, .035f, .42f), new Color(.005f, .005f, .15f), u);
                gradient.SetPixel(x, y, Color.Lerp(bottom, top, v));
            }
            gradient.Apply();
        }

        public void Draw(DialogueController dialogue, float width, float height)
        {
            EnsureStyles();
            if (lastText != dialogue.Text || lastTitle != dialogue.Title)
            { scroll = Vector2.zero; lastText = dialogue.Text; lastTitle = dialogue.Title; }
            float faceSize = Mathf.Clamp(width * .13f, 80, 152);
            float left = 36 + faceSize + 20;
            float textWidth = Mathf.Max(80, width - left - 36);
            // Reserve scrollbar width before measuring, so wrapping stays stable when it appears.
            float contentWidth = textWidth - 20;
            float titleHeight = title.CalcHeight(new GUIContent(dialogue.Title), contentWidth);
            float bodyHeight = Mathf.Max(64, body.CalcHeight(new GUIContent(dialogue.Text), contentWidth));
            float bodyTop = titleHeight + 8, choicesTop = bodyTop + bodyHeight + 12;
            float hintTop = choicesTop;
            var options = dialogue.Choices;
            if (dialogue.HasChoices)
                for (int i = 0; i < options.Count; i++) hintTop += ChoiceHeight(options[i], i, contentWidth) + 6;
            string controls = dialogue.HasChoices ? "1 / 2 / 3 ou clique: escolher  |  Esc: cancelar"
                : "E / Espaco / Enter: continuar  |  Esc: fechar  (" + dialogue.PageNumber + "/" + dialogue.PageCount + ")";
            float hintHeight = hint.CalcHeight(new GUIContent(controls), contentWidth);
            float contentHeight = hintTop + 8 + hintHeight;
            float panelHeight = Mathf.Min(height - 32, Mathf.Max(faceSize, contentHeight) + 40);
            Rect panel = new Rect(16, height - 16 - panelHeight, width - 32, panelHeight);
            Panel(panel);
            Rect face = new Rect(36, panel.y + 20, faceSize, faceSize);
            Panel(face);
            DrawPortrait(Inset(face, 10), dialogue.Title);

            Rect viewport = new Rect(left, panel.y + 20, textWidth, panel.height - 40);
            scroll = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, contentWidth, contentHeight), false, false);
            try
            {
                ShadowLabel(new Rect(0, 0, contentWidth, titleHeight), dialogue.Title, title);
                ShadowLabel(new Rect(0, bodyTop, contentWidth, bodyHeight), dialogue.Text, body);
                float y = choicesTop;
                if (dialogue.HasChoices)
                {
                    // Callbacks may replace or close the conversation. Finish this GUI pass immediately.
                    for (int i = 0; i < options.Count; i++)
                    {
                        float optionHeight = ChoiceHeight(options[i], i, contentWidth);
                        Rect button = new Rect(0, y, contentWidth, optionHeight);
                        Fill(button, new Color(.52f, .60f, .79f));
                        Fill(Inset(button, 1), new Color(.035f, .09f, .30f));
                        if (GUI.Button(button, (i + 1) + ". " + options[i], choice)) { dialogue.Choose(i); break; }
                        y += optionHeight + 6;
                    }
                }
                ShadowLabel(new Rect(0, hintTop + 8, contentWidth, hintHeight), controls, hint);
            }
            finally { GUI.EndScrollView(); }
        }

        public void DrawPrompt(string prompt, float width, float height)
        {
            if (string.IsNullOrEmpty(prompt)) return;
            EnsureStyles();
            float promptWidth = Mathf.Min(400, width - 32);
            float promptHeight = Mathf.Max(48, body.CalcHeight(new GUIContent(prompt), promptWidth - 32) + 32);
            Rect area = new Rect((width - promptWidth) / 2, height - 16 - promptHeight, promptWidth, promptHeight);
            Panel(area);
            ShadowLabel(Inset(area, 16), prompt, body);
        }

        private float ChoiceHeight(string text, int index, float width)
            => Mathf.Max(38, choice.CalcHeight(new GUIContent((index + 1) + ". " + text), width));

        private void DrawPortrait(Rect area, string speaker)
        {
            bool dragon = speaker == "Dragao corrompido" || speaker == "Essencia liberta" || speaker == "Essencia do dragao";
            string npc = speaker == "Guardiao da pedreira" ? "Lenhador" : speaker == "Hospedaria" ? "Hospedeira" : speaker;
            int frame = PixelSpriteLibrary.NpcFrame(npc);
            bool isNpc = frame != 0 || npc == "Aldeao";
            string resource = dragon ? "Dragon" : isNpc ? "NpcRoster" : "ArenWalk";
            if (!portraits.TryGetValue(resource, out var atlas))
            {
                atlas = new PixelSpriteAtlas(resource);
                portraits.Add(resource, atlas);
            }
            if (dragon) { atlas.Draw(area, 0, Color.white); return; }
            Rect source = atlas.Sprites[isNpc ? frame : 0].rect;
            // Crop a square bust from the existing sprite, without stretching or changing its PNG.
            float side = Mathf.Min(source.width, source.height * .64f);
            Rect uv = new Rect((source.center.x - side / 2) / atlas.Texture.width,
                (source.yMax - side) / atlas.Texture.height, side / atlas.Texture.width, side / atlas.Texture.height);
            GUI.DrawTextureWithTexCoords(area, atlas.Texture, uv, true);
        }

        private void EnsureStyles()
        {
            if (title != null) return;
            title = PrototypeTypography.Style(GUI.skin.label, 28, true);
            body = PrototypeTypography.Style(GUI.skin.label, 24);
            hint = PrototypeTypography.Style(GUI.skin.label, 18);
            choice = PrototypeTypography.Style(GUI.skin.label, 24);
            foreach (GUIStyle style in new[] { title, body, hint, choice })
            {
                style.wordWrap = true;
                style.padding = new RectOffset(0, 0, 0, 0);
                style.normal.textColor = style.hover.textColor = style.active.textColor = Color.white;
            }
            choice.padding = new RectOffset(12, 12, 7, 7);
            choice.alignment = TextAnchor.MiddleLeft;
            choice.hover.textColor = choice.active.textColor = new Color(1, .94f, .66f);
            hint.normal.textColor = new Color(.78f, .84f, 1);
        }

        private void Panel(Rect area)
        {
            Fill(area, new Color(.15f, .16f, .18f));
            Fill(Inset(area, 2), new Color(.62f, .65f, .65f));
            Fill(Inset(area, 4), new Color(.92f, .93f, .86f));
            Fill(Inset(area, 6), new Color(.29f, .31f, .32f));
            GUI.DrawTexture(Inset(area, 8), gradient);
        }
        private static Rect Inset(Rect rect, float amount) => new Rect(rect.x + amount, rect.y + amount,
            rect.width - amount * 2, rect.height - amount * 2);
        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color; GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous;
        }
        private static void ShadowLabel(Rect rect, string text, GUIStyle style)
        {
            Color previous = GUI.color; GUI.color = new Color(.05f, .06f, .13f);
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, style);
            GUI.color = previous; GUI.Label(rect, text, style);
        }
        public void Dispose()
        {
            foreach (var portrait in portraits.Values) portrait.Dispose();
            UnityEngine.Object.Destroy(gradient);
        }
    }
}
