using UnityEngine;

namespace Drift
{
    public sealed class AnchorPortal : Interactable
    {
        [SerializeField]
        private AnchorController anchor;
        [SerializeField]
        private GameFlowManager gameFlow;
        public override string Prompt => "E — ESCAPE";
        public override bool CanInteract => base.CanInteract && anchor != null && anchor.IsRestored && gameFlow != null && gameFlow.Phase == GameFlowManager.GamePhase.FinalEscape && !gameFlow.IsPaused;

        public override void Interact(PlayerInteraction player)
        {
            if (CanInteract)
                gameFlow.EscapeThroughPortal();
        }
    }
}
