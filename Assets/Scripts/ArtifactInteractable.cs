using UnityEngine;

namespace Drift
{
    public sealed class ArtifactInteractable : Interactable
    {
        [SerializeField]
        private bool corrupted;
        [SerializeField]
        private OpeningSequence openingSequence;
        public override string Prompt => corrupted ? base.Prompt : null;
        public override bool CanInteract => base.CanInteract && (!corrupted || (openingSequence != null && !openingSequence.HasArrived && !openingSequence.IsTransitioning));

        public override void Interact(PlayerInteraction player)
        {
            if (!CanInteract)
                return;
            if (corrupted)
                openingSequence.BeginTransition();
        }
    }
}
