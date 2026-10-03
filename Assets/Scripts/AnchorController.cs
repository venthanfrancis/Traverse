using UnityEngine;

namespace Drift
{
    public sealed class AnchorController : MonoBehaviour
    {
        [SerializeField]
        private GameFlowManager gameFlow;
        [SerializeField]
        private Light anchorLight;
        [SerializeField]
        private ParticleSystem anchorParticles;
        [SerializeField]
        private GameObject[] escapeBarriers;
        [SerializeField]
        private GameObject[] collapseBarriers;
        [SerializeField]
        private Transform escapeCheckpoint;
        [SerializeField]
        private PlayerRespawn playerRespawn;
        private readonly bool[] nodes = new bool[3];
        public int ActiveNodes { get; private set; }
        public bool IsRestored => ActiveNodes == 3;

        public bool IsNodeActive(int index) => index >= 0 && index < nodes.Length && nodes[index];
        public bool ActivateNode(int index)
        {
            if (index < 0 || index >= nodes.Length || nodes[index] || index != ActiveNodes)
                return false;
            nodes[index] = true;
            ActiveNodes++;
            if (anchorLight != null)
                anchorLight.intensity = 2f + ActiveNodes * 2f;
            if (IsRestored)
            {
                foreach (GameObject barrier in escapeBarriers)
                    if (barrier != null)
                        barrier.SetActive(false);
                foreach (GameObject barrier in collapseBarriers)
                    if (barrier != null)
                        barrier.SetActive(true);
                Physics.SyncTransforms();
                if (anchorParticles != null)
                    anchorParticles.Play();
                playerRespawn.SetCheckpoint(escapeCheckpoint);
                gameFlow.BeginEscape();
            }

            return true;
        }
    }
}
