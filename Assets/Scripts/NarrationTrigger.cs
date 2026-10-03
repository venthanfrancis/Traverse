using UnityEngine;
using static NarrationSequence;

namespace Drift
{
    // Plays a narration beat when the player walks into this trigger volume.
    // Other game events can call Fire() directly (UnityEvent, ArtifactInteractable, etc.).
    // Prefer one trigger per beat; if several share a beat, only the first one plays it.
    [RequireComponent(typeof(Collider))]
    public sealed class NarrationTrigger : MonoBehaviour
    {
        [SerializeField]
        private NarrationBeats beat = NarrationBeats.None;
        [SerializeField, Min(0f)]
        private float delay;
        [SerializeField]
        private bool once = true;
        private bool fired;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerMovement>() == null)
                return;
            Fire();
        }

        public void Fire()
        {
            if (fired && once)
                return;
            if (beat == NarrationBeats.None)
            {
                Debug.LogWarning("NarrationTrigger has no beat assigned.", this);
                return;
            }

            fired = true;
            // "Once" is per beat for the whole session, so duplicated triggers (or a code hook
            // announcing the same beat) never repeat a line.
            NarrationManager.Announce(beat, delay, once);
        }

        private void OnDrawGizmos()
        {
            Collider c = GetComponent<Collider>();
            if (c == null)
                return;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            if (c is BoxCollider box)
                Gizmos.DrawCube(box.center, box.size);
            else if (c is SphereCollider sphere)
                Gizmos.DrawSphere(sphere.center, sphere.radius);
        }
    }
}
