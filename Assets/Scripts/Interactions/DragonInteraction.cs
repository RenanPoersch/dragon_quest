using DragonQuest.Presentation.World.Animations;
using System;
using UnityEngine;
using DragonQuest.Progression;
using DragonQuest.Content;

namespace DragonQuest.Interactions
{
    public sealed class DragonInteraction : Interactable
    {
        public const float BattleMusicLeadSeconds = 1f;
        private DragonProgress progress;
        private Action startBattle;
        private GameObject body, essence;
        private DragonLairAnimation restingDragon;
        private DialogueController battleDialogue;
        private int finalPageVersion, encounterPageCount;

        public bool IsBattlePrelude => isActiveAndEnabled && progress?.IsDefeated == false
            && battleDialogue != null && battleDialogue.IsOpen
            && battleDialogue.ContentVersion == finalPageVersion
            && battleDialogue.PageCount == encounterPageCount && battleDialogue.PageNumber == encounterPageCount
            && battleDialogue.RevealSecondsRemaining <= BattleMusicLeadSeconds;

        public void Initialize(DragonProgress dragon, Action start, GameObject model, GameObject glow)
        {
            progress = dragon; startBattle = start; body = model; essence = glow;
            ClearPrelude();
            restingDragon = body.GetComponent<DragonLairAnimation>();
            restingDragon?.ResetRest(); Refresh();
        }

        private void OnEnable() { ClearPrelude(); restingDragon?.ResetRest(); Refresh(); }
        private void OnDisable() => ClearPrelude();
        private void ClearPrelude() { battleDialogue = null; finalPageVersion = encounterPageCount = 0; }
        private void Update() => Refresh();
        private void Refresh()
        {
            if (progress == null) return;
            body.SetActive(!progress.IsDefeated); essence.SetActive(progress.IsDefeated);
        }

        public override void Interact(InteractionContext context)
        {
            ClearPrelude();
            if (progress.IsDefeated)
            {
                context.Dialogue.Open(DialogueCatalog.FreedDragonEssence);
                return;
            }
            bool wasSleeping = restingDragon?.State == DragonRestState.Sleeping;
            restingDragon?.Wake();
            context.Dialogue.Open(DialogueCatalog.DragonConfrontation(wasSleeping), closed: () =>
            {
                ClearPrelude();
                // A fast dialogue advance still switches to the awake pose before battle.
                restingDragon?.FinishAwakening();
                startBattle?.Invoke();
            });
            battleDialogue = context.Dialogue;
            encounterPageCount = battleDialogue.PageCount;
            finalPageVersion = battleDialogue.ContentVersion + encounterPageCount - 1;
        }
    }
}
