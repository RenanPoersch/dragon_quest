using DragonQuest.Content;
using System;
using UnityEngine;
using DragonQuest.Progression;

// Coordena o encontro com o Rei Rubro, incluindo a apresentação do grupo e o início do combate.
namespace DragonQuest.Interactions
{
    public sealed class TyrantInteraction : Interactable
    {
        // Obtém o grupo atual no momento em que a interação é executada.
        private Func<PartyProgress> getParty;
        private Action startBattle;
        // Entrega o controlador de conversa à rotina de audiência.
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
