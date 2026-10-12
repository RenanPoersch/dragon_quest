using UnityEngine;

namespace DragonQuest.Interactions
{
    public sealed class BlockedPassageInteraction : Interactable
    {
        public override string Prompt => "Examinar passagem entre as rochas";
        private GameObject barrier;
        private float openingTime;
        private bool opening;
        private Progression.AncientSecrets secrets;
        private bool fullyOpen;
        public void Initialize(GameObject rocks, Progression.AncientSecrets progress = null) { barrier=rocks; secrets=progress; }
        public override void Interact(InteractionContext context)
        {
            if(context.Secrets.PassageOpen)
                context.Dialogue.Open("Trilha antiga","Os selos estao desfeitos. A trilha ate o vulcao permanece livre.");
            else if(context.Secrets.TryOpenPassage())
            {
                barrier.GetComponent<BoxCollider2D>().enabled=false;
                Physics2D.SyncTransforms(); opening=true; openingTime=0;
                context.Dialogue.Open("Passagem oculta","A Escama ancestral vibra. Os tres selos se desfazem e as rochas se afastam, revelando a trilha do vulcao.",
                    "A escama permanece com a equipe. Um sopro quente desce das montanhas.");
            }
            else context.Dialogue.Open("Passagem oculta","Uma trilha estreita segue por entre as montanhas ate o vulcao.",
                context.Secrets.HasScale?"A escama vibra, mas alguns selos ainda resistem.":"As rochas estao marcadas com tres selos. A passagem permanece bloqueada.");
        }
        private void Update()
        {
            if (!opening && !fullyOpen && secrets?.PassageOpen == true && barrier != null)
            {
                barrier.GetComponent<BoxCollider2D>().enabled = false;
                SetOffset(2.35f); fullyOpen = true; Physics2D.SyncTransforms();
            }
            if(!opening || barrier==null) return;
            openingTime+=Time.unscaledDeltaTime;
            float offset=Mathf.SmoothStep(0.75f,2.35f,Mathf.Clamp01(openingTime/0.8f));
            SetOffset(offset);
            if(openingTime>=0.8f) { opening=false; fullyOpen=true; }
        }
        private void SetOffset(float offset)
        {
            for(int i=0;i<barrier.transform.childCount;i++)
                barrier.transform.GetChild(i).localPosition=new Vector3(i==0?-offset:offset,0,0);
        }
    }
}
