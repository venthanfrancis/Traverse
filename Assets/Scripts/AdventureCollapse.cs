using UnityEngine;
using Beat = NarrationSequence.NarrationBeats;

namespace Drift
{
    // One shared timer drives both realities, so Q cannot reset a collapsing bridge.
    public sealed class AdventureCollapse : MonoBehaviour
    {
        [SerializeField]
        private Transform[] slabs;
        private const int SlabsPerWorld = 3;
        private const float CollapseDelay = 3.5f;
        private const float SlabDelay = .65f;
        private const float RebuildDelay = 12f;
        private Vector3[] positions;
        private Collider[] colliders;
        private GameFlowManager flow;
        private float age = -1;
        public bool IsRunning => age >= 0;

        private void Start()
        {
            flow = FindFirstObjectByType<GameFlowManager>();
            positions = new Vector3[slabs.Length];
            colliders = new Collider[slabs.Length];
            for (int i = 0; i < slabs.Length; i++)
            {
                positions[i] = slabs[i].localPosition;
                colliders[i] = slabs[i].GetComponent<Collider>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (age >= 0 || flow == null || flow.Phase != GameFlowManager.GamePhase.FinalEscape)
                return;
            PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
            if (player == null)
                return;
            age = 0;
            NarrationManager.Announce(Beat.CollapseWarning);
        }

        private void Update()
        {
            if (age < 0)
                return;
            age += Time.deltaTime;
            for (int i = 0; i < slabs.Length; i++)
            {
                int slabIndex = i % SlabsPerWorld;
                float fall = Mathf.Max(0, age - CollapseDelay - slabIndex * SlabDelay);
                slabs[i].localPosition = positions[i] + Vector3.down * Mathf.Min(14, fall * fall * 3);
                colliders[i].enabled = fall < .2f;
            }

            // Automatically rebuild for a checkpoint retry; never permanently strand the player.
            if (age > RebuildDelay)
                Restore();
        }

        public void Restore()
        {
            if (positions == null)
                return;
            for (int i = 0; i < slabs.Length; i++)
            {
                slabs[i].localPosition = positions[i];
                colliders[i].enabled = true;
            }

            age = -1;
        }
    }
}
