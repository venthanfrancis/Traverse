using UnityEngine;
using Beat = NarrationSequence.NarrationBeats;

namespace Drift
{
    public sealed class EnergyNode : Interactable
    {
        [SerializeField]
        private AnchorController anchor;
        [SerializeField, Range(0, 2)]
        private int nodeIndex;
        [SerializeField]
        private GameObject[] unlockBarriers;
        [SerializeField]
        private Renderer visual;
        private bool IsNext => anchor != null && anchor.ActiveNodes == nodeIndex;

        public override string Prompt => IsNext ? "E — RESTORE" : "E — LOCKED";
        // Still interactable out of order so the player gets feedback instead of silence.
        public override bool CanInteract => base.CanInteract && anchor != null && !anchor.IsNodeActive(nodeIndex);

        public override void Interact(PlayerInteraction player)
        {
            if (!CanInteract)
                return;
            if (!IsNext)
            {
                player.ShowMessage("Restore the earlier anchor first", 2f);
                NarrationManager.Announce(Beat.NodeWrongOrder);
                return;
            }

            if (!anchor.ActivateNode(nodeIndex))
                return;
            foreach (GameObject barrier in unlockBarriers)
                if (barrier != null)
                    barrier.SetActive(false);
            Physics.SyncTransforms();
            if (visual != null)
            {
                MaterialPropertyBlock color = new MaterialPropertyBlock();
                color.SetColor("_BaseColor", Color.cyan);
                color.SetColor("_EmissionColor", Color.cyan * 3f);
                visual.SetPropertyBlock(color);
            }
        }
    }
}
