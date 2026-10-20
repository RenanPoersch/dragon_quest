using System;
using UnityEngine;
using DragonQuest.Equipment;
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
            context.Dialogue.OpenWithCompletion("Rei Rubro", startBattle,
                "Todo caminho nesta ilha termina diante do meu trono. Curvem-se, ou o próprio céu cairá sobre vocês!");
            presentParty?.Invoke(context.Dialogue);
        }
    }
}
