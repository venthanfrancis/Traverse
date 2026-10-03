using UnityEngine;

namespace Drift
{
    public sealed class SignalPresentation : MonoBehaviour
    {
        [SerializeField]
        private AnchorController anchor;
        [SerializeField]
        private int nodeIndex = -1;
        [SerializeField]
        private PuzzleDoor door;
        [SerializeField]
        private PlayerRespawn respawn;
        [SerializeField]
        private Transform checkpoint;
        [SerializeField]
        private AudioClip activatedSound;
        private AudioSource source;
        private bool active, initialized;
        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1;
            source.maxDistance = 18;
            source.volume = .25f;
        }

        private void Update()
        {
            bool now = anchor != null ? anchor.IsNodeActive(nodeIndex) : door != null ? door.IsOpen : respawn != null && respawn.CurrentCheckpoint == checkpoint;
            if (initialized && now == active)
                return;
            bool cue = initialized && now && !active;
            active = now;
            initialized = true;
            if (cue && activatedSound != null)
                source.PlayOneShot(activatedSound);
        }
    }
}
