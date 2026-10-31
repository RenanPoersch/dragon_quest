using DragonQuest.Progression;
using DragonQuest.Content;
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
        public override bool CanInteract => base.CanInteract && secrets?.PassageOpen != true;
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
            if(!CanInteract || context.Secrets.PassageOpen) return;
            if(context.Secrets.TryOpenPassage())
            {
                barrier.GetComponent<Collider2D>().enabled=false;
                Physics2D.SyncTransforms(); opening=true; openingTime=0;
                context.Dialogue.Open(DialogueCatalog.PassageOpened);
            }
            else
            {
                context.Dialogue.Open(context.Secrets.HasScale ? DialogueCatalog.PassageResisting : DialogueCatalog.PassageSealed);
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
