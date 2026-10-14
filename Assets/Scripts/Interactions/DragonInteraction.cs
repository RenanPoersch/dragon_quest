using System;
using UnityEngine;
using DragonQuest.Progression;
using DragonQuest.Prototype;

namespace DragonQuest.Interactions
{
    public sealed class DragonInteraction : Interactable
    {
        private DragonProgress progress;
        private Action startBattle;
        private GameObject body, essence;
        private DragonLairAnimation restingDragon;
        public override string Prompt => progress?.IsDefeated == true ? "Ouvir a essencia liberta"
            : restingDragon?.State == DragonRestState.Sleeping ? "Acordar o dragao" : "Enfrentar o dragao corrompido";

        public void Initialize(DragonProgress dragon, Action start, GameObject model, GameObject glow)
        {
            progress = dragon; startBattle = start; body = model; essence = glow;
            restingDragon = body.GetComponent<DragonLairAnimation>();
            restingDragon?.ResetRest(); Refresh();
        }

        private void OnEnable() { restingDragon?.ResetRest(); Refresh(); }
        private void Update() => Refresh();
        private void Refresh()
        {
            if (progress == null) return;
            body.SetActive(!progress.IsDefeated); essence.SetActive(progress.IsDefeated);
        }

        public override void Interact(InteractionContext context)
        {
            if (progress.IsDefeated)
            {
                context.Dialogue.Open("Essencia liberta", "Minha chama esta livre. Leve minha armadura ao castelo; a sombra que me aprisionou ainda fala por seu rei.");
                return;
            }
            bool wasSleeping = restingDragon?.State == DragonRestState.Sleeping;
            restingDragon?.Wake();
            context.Dialogue.OpenWithCompletion("Dragao corrompido", () =>
            {
                // A fast dialogue advance still switches to the awake pose before battle.
                restingDragon?.FinishAwakening();
                startBattle?.Invoke();
            },
                wasSleeping ? "O dragao dorme sobre o basalto quente. Ao ouvir seus passos, abre os olhos, ergue a cabeca e estende as asas. O covil estremece com seu rugido."
                    : "O dragao acompanha seus movimentos. Uma sombra pulsa entre as escamas, e uma segunda voz parece vir de sua garganta.",
                "O calor se intensifica. Prepare sua equipe: Gelo pode romper suas escamas; quando ele concentrar chamas, defenda-se!");
        }
    }
}
