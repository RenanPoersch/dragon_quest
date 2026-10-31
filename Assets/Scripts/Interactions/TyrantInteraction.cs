using DragonQuest.Content;
using System;
using UnityEngine;
using DragonQuest.Progression;

namespace DragonQuest.Interactions
{
    public sealed class TyrantInteraction : Interactable
    {
        private Func<PartyProgress> getParty;
        private Action startBattle;
        private Action<DialogueController> presentParty;
        private GameObject spirit;
        public void Initialize(Func<PartyProgress> party,Action start,GameObject spiritModel,Action<DialogueController> partyPresentation=null)
        {getParty=party;startBattle=start;spirit=spiritModel;presentParty=partyPresentation;}
        private bool Revealed()
        {
            PartyProgress party=getParty?.Invoke();
            if(party==null) return false;
            foreach(PartyMember member in party.Members) if(member.Armor?.Definition==DragonProgress.Armor) return true;
            return false;
        }
        private void Update()
        {
            if(spirit!=null) spirit.SetActive(Revealed());
        }
        public override void Interact(InteractionContext context)
        {
            context.Dialogue.Open(DialogueCatalog.KingConfrontation, startBattle);
            presentParty?.Invoke(context.Dialogue);
        }
    }
}
