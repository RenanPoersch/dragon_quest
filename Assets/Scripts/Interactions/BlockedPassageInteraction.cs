using UnityEngine;

namespace DragonQuest.Interactions
{
    public sealed class BlockedPassageInteraction : Interactable
    {
        private GameObject barrier;
        private float openingTime;
        private bool opening;
        private Progression.AncientSecrets secrets;
        private bool fullyOpen;
        public void Initialize(GameObject ward, Progression.AncientSecrets progress = null)
        { barrier=ward; secrets=progress; RestoreBarrierState(); }
        public void RestoreBarrierState()
        {
            if (barrier == null) return;
            opening = false; fullyOpen = secrets?.PassageOpen == true;
            barrier.transform.localScale = Vector3.one;
            barrier.GetComponent<Collider2D>().enabled = !fullyOpen;
            barrier.SetActive(!fullyOpen); Physics2D.SyncTransforms();
        }
        public override void Interact(InteractionContext context)
        {
            if(context.Secrets.PassageOpen)
                context.Dialogue.Open("Trilha antiga","Os selos estao desfeitos. A trilha ate o vulcao permanece livre.");
            else if(context.Secrets.TryOpenPassage())
            {
                barrier.GetComponent<Collider2D>().enabled=false;
                Physics2D.SyncTransforms(); opening=true; openingTime=0;
                context.Dialogue.Open("Barreira ancestral","A Escama ancestral vibra. Os tres selos se desfazem e a barreira magica ao redor do vulcao se dissipa.",
                    "A escama permanece com a equipe. O caminho ate a cratera esta livre.");
            }
            else
            {
                const string description = "Uma barreira mágica envolve o vulcão. Três selos brilham sobre a superfície.";
                if(context.Secrets.HasScale)
                    context.Dialogue.Open("Barreira ancestral",description,"A escama vibra, mas alguns selos ainda resistem.");
                else context.Dialogue.Open("Barreira ancestral",description);
            }
        }
        private void Update()
        {
            if (barrier == null) return;
            if (secrets?.PassageOpen != true)
            {
                if (opening || fullyOpen)
                {
                    opening = fullyOpen = false; barrier.SetActive(true); barrier.transform.localScale = Vector3.one;
                    barrier.GetComponent<Collider2D>().enabled = true; Physics2D.SyncTransforms();
                }
                return;
            }
            if (!opening && !fullyOpen && secrets?.PassageOpen == true && barrier != null)
            {
                barrier.GetComponent<Collider2D>().enabled = false;
                barrier.SetActive(false); fullyOpen = true; Physics2D.SyncTransforms();
            }
            if(!opening || barrier==null) return;
            openingTime+=Time.unscaledDeltaTime;
            float height = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(openingTime / 0.8f));
            barrier.transform.localScale = new Vector3(1, 1, height);
            if(openingTime>=0.8f) { opening=false; fullyOpen=true; barrier.SetActive(false); }
        }
    }
}
