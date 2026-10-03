using UnityEngine;

namespace Drift
{
    public sealed class ArtifactInteractable : Interactable
    {
        [SerializeField]
        private bool corrupted;
        [SerializeField]
        private OpeningSequence openingSequence;

        // The corrupted orb starts the player in Broken World, the stable orb in Normal World.
        private RealityManager.Dimension StartDimension => corrupted ? RealityManager.Dimension.Broken : RealityManager.Dimension.Normal;

        // OpeningSequence lives in MainMenu, so the stable orb finds it at runtime instead of through a scene link.
        private OpeningSequence Opening
        {
            get
            {
                if (openingSequence == null)
                    openingSequence = FindFirstObjectByType<OpeningSequence>();
                return openingSequence;
            }
        }

        public override bool CanInteract => base.CanInteract && Opening != null && !Opening.HasArrived && !Opening.IsTransitioning;

        public override void Interact(PlayerInteraction player)
        {
            if (!CanInteract)
                return;
            // The corrupted orb uses the opening's own light and particles; the stable orb uses its own.
            Light light = corrupted ? null : GetComponentInChildren<Light>();
            ParticleSystem particles = corrupted ? null : GetComponentInChildren<ParticleSystem>();
            Opening.BeginTransition(gameObject, StartDimension, light, particles);
        }
    }
}
