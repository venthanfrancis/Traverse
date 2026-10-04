using UnityEngine;

namespace Drift
{
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField]
        private Transform spawnPoint;
        [SerializeField]
        private bool showMessage = true;
        private bool reached;
        private void OnTriggerEnter(Collider other)
        {
            PlayerRespawn player = other.GetComponentInParent<PlayerRespawn>();
            if (player == null || reached)
                return;
            reached = true;
            player.SetCheckpoint(spawnPoint != null ? spawnPoint : transform);
            if (showMessage)
                player.GetComponent<PlayerInteraction>()?.ShowMessage("Checkpoint", 2f);
        }
    }
}
