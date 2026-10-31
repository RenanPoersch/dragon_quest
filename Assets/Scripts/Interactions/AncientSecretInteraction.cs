using UnityEngine;
using DragonQuest.Audio;
using DragonQuest.Progression;
using DragonQuest.Content;

namespace DragonQuest.Interactions
{
    public enum SecretObject { Peasant, Altar, Inscription, Monolith, ClawStone, HollowTree }

    public sealed class AncientSecretInteraction : Interactable
    {
        private SecretObject kind;
        private MonolithSymbol symbol;
        private AncientSecrets secrets;
        private GameObject light;
        public override bool CanInteract => base.CanInteract
            && !(kind == SecretObject.Altar && secrets?.AltarSolved == true)
            && !(kind == SecretObject.HollowTree && secrets?.HasScale == true);
        public void Initialize(SecretObject type, AncientSecrets progress, MonolithSymbol rune = MonolithSymbol.Sun, GameObject glow = null)
        { kind=type; secrets=progress; symbol=rune; light=glow; RefreshLight(); }

        private void OnEnable() => RefreshLight();
        private void Update() => RefreshLight();
        private void RefreshLight()
        {
            if(light==null||secrets==null) return;
            bool visible=kind==SecretObject.Altar?secrets.AltarSolved:kind==SecretObject.Monolith&&secrets.IsLit(symbol);
            if(light.activeSelf!=visible) light.SetActive(visible);
        }
        public override void Interact(InteractionContext context)
        {
            if (!CanInteract) return;
            DialogueController dialogue=context.Dialogue;
            if(kind==SecretObject.Peasant)
            {
                dialogue.Open(DialogueCatalog.PeasantLegend, secrets.LearnPoem);
            }
            else if(kind==SecretObject.Altar)
            {
                if(!secrets.KnowsPoem) dialogue.Open(DialogueCatalog.UnknownAltar);
                else dialogue.Open(DialogueCatalog.AltarVerse, selected: index=>
                {
                    if(secrets.TrySolveAltar(DialogueCatalog.AltarVerse.Choices[index]))
                        dialogue.Open(DialogueCatalog.AltarAwakened);
                    else dialogue.Open(DialogueCatalog.AltarSilent);
                    RefreshLight();
                });
            }
            else if(kind==SecretObject.Inscription)
                dialogue.Open(DialogueCatalog.AncientInscription);
            else if(kind==SecretObject.Monolith)
            {
                MonolithResult result=secrets.Activate(symbol);
                dialogue.Open(DialogueCatalog.Monolith(symbol, result));
                RefreshLight();
            }
            else if(kind==SecretObject.ClawStone)
                dialogue.Open(DialogueCatalog.ClawStone);
            else
                dialogue.Open(DialogueCatalog.HollowTreeScale, () =>
                {
                    if(!secrets.TryCollectScale()) return;
                    MenuSounds.Play(MenuSound.Confirm);
                    dialogue.Open(DialogueCatalog.AncestralScaleReceived);
                });
        }
    }
}
