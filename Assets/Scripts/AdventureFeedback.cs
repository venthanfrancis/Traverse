using UnityEngine;

namespace Drift
{
    public sealed class AdventureFeedback : MonoBehaviour
    {
        [SerializeField]
        private Renderer[] beacons;
        [SerializeField]
        private Light[] beaconLights;
        [SerializeField]
        private AudioClip summitSound;
        private AnchorController anchor;
        private GameFlowManager flow;
        private ObservatoryTelescope observatory;
        private int previousCount;
        private float progressUntil, summitAge = -1;
        private MaterialPropertyBlock colors;
        public bool SummitReached { get; private set; }

        private void Start()
        {
            anchor = FindFirstObjectByType<AnchorController>();
            flow = FindFirstObjectByType<GameFlowManager>();
            observatory = FindFirstObjectByType<ObservatoryTelescope>(FindObjectsInactive.Include);
            colors = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (anchor == null || flow == null)
                return;
            if (anchor.ActiveNodes != previousCount)
            {
                previousCount = anchor.ActiveNodes;
                progressUntil = Time.time + 5;
            }

            if (summitAge >= 0)
                summitAge += Time.deltaTime;
            float brightness = 1 + anchor.ActiveNodes * 1.5f + (observatory != null && observatory.Activated ? 1 : 0);
            float pulse = summitAge >= 0 && summitAge < 3 ? Mathf.Sin(summitAge / 3 * Mathf.PI) * 4 : 0;
            colors.SetColor("_EmissionColor", new Color(.15f, .9f, 1) * (brightness + pulse));
            foreach (Renderer beacon in beacons)
                if (beacon != null)
                    beacon.SetPropertyBlock(colors);
            foreach (Light lamp in beaconLights)
                if (lamp != null)
                    lamp.intensity = brightness + pulse;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (SummitReached || anchor == null || !anchor.IsRestored || flow.Phase != GameFlowManager.GamePhase.FinalEscape)
                return;
            PlayerInteraction visitor = other.GetComponentInParent<PlayerInteraction>();
            if (visitor == null)
                return;
            SummitReached = true;
            summitAge = 0;
            if (summitSound != null)
                GetComponent<AudioSource>().PlayOneShot(summitSound, .65f);
        }

        private void OnGUI()
        {
            if (flow == null || !flow.IsGameplayRunning || Time.time > progressUntil || previousCount == 0)
                return;
            GUI.Box(new Rect(Screen.width * .5f - 130, 70, 260, 32), "ANCHORS RESTORED: " + previousCount + "/3");
        }
    }
}
