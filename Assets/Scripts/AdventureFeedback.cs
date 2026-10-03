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
        private float summitAge = -1;
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

        // The current goal for each phase of the game; null hides the panel.
        private string Objective()
        {
            switch (flow.Phase)
            {
                case GameFlowManager.GamePhase.Opening:
                    return "Touch an orb";
                case GameFlowManager.GamePhase.BrokenWorld:
                    return "Restore the anchors: " + anchor.ActiveNodes + "/3";
                case GameFlowManager.GamePhase.FinalEscape:
                    return "Reach the Anchor and escape";
                case GameFlowManager.GamePhase.EndingWalk:
                    return "Walk back to where you began";
                default:
                    return null;
            }
        }

        private void OnGUI()
        {
            if (flow == null || anchor == null || !flow.IsGameplayRunning)
                return;
            string objective = Objective();
            if (objective == null)
                return;
            Rect panel = new Rect(Hud.Px(24f), Hud.Px(24f), Hud.Px(640f), Hud.Px(104f));
            Hud.Panel(panel);
            Hud.Label(new Rect(panel.x, panel.y + Hud.Px(8f), panel.width, Hud.Px(30f)), "OBJECTIVE", 22f);
            Hud.Label(new Rect(panel.x, panel.y + Hud.Px(40f), panel.width, Hud.Px(56f)), objective, 34f);
        }
    }
}
