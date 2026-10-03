using UnityEngine;
using Beat = NarrationSequence.NarrationBeats;

namespace Drift
{
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField]
        private Transform spawnPoint;
        private bool reached;
        private void OnTriggerEnter(Collider other)
        {
            PlayerRespawn player = other.GetComponentInParent<PlayerRespawn>();
            if (player == null || reached)
                return;
            reached = true;
            player.SetCheckpoint(spawnPoint != null ? spawnPoint : transform);
            player.GetComponent<PlayerInteraction>()?.ShowMessage("Checkpoint", 2f);
            NarrationManager.Announce(Beat.CheckpointReached);
        }
    }
}
