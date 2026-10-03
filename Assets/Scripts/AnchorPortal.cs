using UnityEngine;
using Beat = NarrationSequence.NarrationBeats;

namespace Drift
{
    public sealed class AnchorPortal : Interactable
    {
        [SerializeField]
        private AnchorController anchor;
        [SerializeField]
        private GameFlowManager gameFlow;
        [Tooltip("Horizontal distance at which the dormant Anchor explains itself.")]
        [SerializeField, Min(0f)]
        private float hintRadius = 15f;
        private Transform player;
        public override string Prompt => "E — ESCAPE";
        public override bool CanInteract => base.CanInteract && anchor != null && anchor.IsRestored && gameFlow != null && gameFlow.Phase == GameFlowManager.GamePhase.FinalEscape && !gameFlow.IsPaused;

        public override void Interact(PlayerInteraction player)
        {
            if (CanInteract)
                gameFlow.EscapeThroughPortal();
        }

        // The Anchor cannot be interacted with until all three nodes are restored, so it
        // would otherwise do nothing at all when the player walks up to it.
        private void Update()
        {
            if (anchor == null || anchor.IsRestored)
                return;
            if (player == null)
            {
                PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
                if (movement == null)
                    return;
                player = movement.transform;
            }

            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= hintRadius * hintRadius)
                NarrationManager.Announce(Beat.AnchorDormant);
        }
    }
}
