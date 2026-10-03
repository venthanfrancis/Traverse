using UnityEngine;

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
        public override string Prompt => "E — RESTORE";
        public override bool CanInteract => base.CanInteract && anchor != null && !anchor.IsNodeActive(nodeIndex) && anchor.ActiveNodes == nodeIndex;

        public override void Interact(PlayerInteraction player)
        {
            if (!CanInteract || !anchor.ActivateNode(nodeIndex))
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
