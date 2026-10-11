using System;
using UnityEngine;
using DragonQuest.Progression;

namespace DragonQuest.Interactions
{
    public sealed class DragonInteraction : Interactable
    {
        private DragonProgress progress;
        private Action startBattle;
        private GameObject body, essence;
        public override string Prompt => progress?.IsDefeated == true ? "Ouvir a essencia liberta" : "Enfrentar o dragao corrompido";
        public void Initialize(DragonProgress dragon, Action start, GameObject model, GameObject glow)
        { progress=dragon; startBattle=start; body=model; essence=glow; Refresh(); }
        private void Update() => Refresh();
        private void Refresh()
        {
            if(progress==null) return;
            body.SetActive(!progress.IsDefeated); essence.SetActive(progress.IsDefeated);
        }
        public override void Interact(InteractionContext context)
        {
            if(progress.IsDefeated)
                context.Dialogue.Open("Essencia liberta","Minha chama esta livre. Leve minha armadura ao castelo; a sombra que me aprisionou ainda fala por seu rei.");
            else context.Dialogue.OpenWithCompletion("Dragao corrompido",startBattle,
                "Uma sombra pulsa entre as escamas. O dragao ruge, mas uma segunda voz parece vir de sua garganta.",
                "O calor se intensifica. Prepare sua equipe: Gelo pode romper suas escamas; quando ele concentrar chamas, defenda-se!");
        }
    }
}
