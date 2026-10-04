using UnityEngine;

namespace Drift
{
    public sealed class EchoBlockPuzzle : Interactable
    {
        [SerializeField] private Transform[] plates;
        [SerializeField] private Renderer[] plateVisuals;
        [SerializeField] private Renderer[] gateIndicators;
        [SerializeField] private Light[] gateLights;
        [SerializeField] private Transform echo;
        [SerializeField] private PuzzleDoor door;
        [SerializeField] private GameObject exitBarrier;
        [SerializeField] private AudioSource solveSound;
        private RealityManager reality;
        private PlayerInteraction holder;
        private Transform home;
        private Vector3 pickupPosition;
        private Quaternion pickupRotation;
        private Collider blockCollider;
        private MaterialPropertyBlock color;
        public bool HasEcho { get; private set; }
        public bool IsHeld => holder != null;
        public bool IsSolved => door.IsOpen;
        public Vector3 EchoPosition => echo.position;
        public override string Prompt => IsHeld ? "E — PLACE" : "E — PICK UP";
        public override bool CanInteract => base.CanInteract && (!IsHeld || NearbyPlate() >= 0);

        private void Awake()
        {
            color = new MaterialPropertyBlock();
            home = transform.parent;
            blockCollider = GetComponent<Collider>();
            reality = FindFirstObjectByType<RealityManager>(FindObjectsInactive.Include);
            reality.DimensionChanged += WorldChanged;
            echo.gameObject.SetActive(false);
            if (exitBarrier == null)
                foreach (Transform item in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (item.gameObject.scene == gameObject.scene && item.name == "RidgeShortcutGate")
                    {
                        exitBarrier = item.gameObject;
                        break;
                    }
        }

        private void OnDestroy()
        {
            if (reality != null)
                reality.DimensionChanged -= WorldChanged;
        }

        private void WorldChanged(RealityManager.Dimension world)
        {
            // Only the first placement in each Normal visit records a new arrangement.
            if (world == RealityManager.Dimension.Normal)
                HasEcho = false;
        }

        private int NearbyPlate()
        {
            if (!IsHeld)
                return -1;
            int nearest = -1;
            float best = 1.8f * 1.8f;
            for (int i = 0; i < plates.Length; i++)
            {
                Vector3 offset = holder.transform.position - plates[i].position;
                if (Mathf.Abs(offset.y) > 1f)
                    continue;
                offset.y = 0;
                if (offset.sqrMagnitude < best)
                {
                    nearest = i;
                    best = offset.sqrMagnitude;
                }
            }
            return nearest;
        }

        private Vector3 Placement(int index) => plates[index].position + Vector3.up * .5f;

        public override void Interact(PlayerInteraction player)
        {
            if (!CanInteract || !player.InputEnabled)
                return;
            if (!IsHeld)
            {
                holder = player;
                pickupPosition = transform.position;
                pickupRotation = transform.rotation;
                blockCollider.isTrigger = true;
                transform.SetPositionAndRotation(player.transform.TransformPoint(new Vector3(0, 1, 1.1f)), player.transform.rotation);
                return;
            }
            if (player != holder)
                return;
            int index = NearbyPlate();
            transform.SetParent(home, true);
            transform.SetPositionAndRotation(Placement(index), Quaternion.identity);
            blockCollider.isTrigger = false;
            holder = null;
            if (reality.IsDrifting && !HasEcho)
            {
                echo.position = transform.position;
                HasEcho = true;
            }
            Physics.SyncTransforms();
        }

        public void ReleaseCarry()
        {
            if (!IsHeld) return;
            holder = null;
            blockCollider.isTrigger = false;
            transform.SetPositionAndRotation(pickupPosition, pickupRotation);
            Physics.SyncTransforms();
        }

        private void LateUpdate()
        {
            if (Time.timeScale <= 0)
                return;
            if (IsHeld)
            {
                Vector3 start = holder.transform.position + Vector3.up;
                float distance = 1.1f;
                foreach (RaycastHit hit in Physics.SphereCastAll(start, .45f, holder.transform.forward, distance, ~0, QueryTriggerInteraction.Ignore))
                    if (!hit.transform.IsChildOf(holder.transform))
                        distance = Mathf.Min(distance, Mathf.Max(0, hit.distance - .05f));
                transform.SetPositionAndRotation(holder.transform.TransformPoint(new Vector3(0, 1, distance)), holder.transform.rotation);
            }
            bool broken = !reality.IsDrifting;
            echo.gameObject.SetActive(HasEcho && (broken || IsHeld || Vector3.Distance(transform.position, echo.position) > .1f));
            bool both = true;
            for (int i = 0; i < plates.Length; i++)
            {
                Vector3 slot = Placement(i);
                bool pressed = (!IsHeld && Vector3.Distance(transform.position, slot) < .1f) || (broken && HasEcho && Vector3.Distance(echo.position, slot) < .1f);
                both &= pressed;
                bool lit = pressed || door.IsOpen;
                color.SetColor("_BaseColor", lit ? Color.cyan : new Color(.35f, .25f, .15f));
                color.SetColor("_EmissionColor", lit ? Color.cyan * 2f : Color.black);
                plateVisuals[i].SetPropertyBlock(color);
                if (gateIndicators != null && i < gateIndicators.Length && gateIndicators[i] != null)
                    gateIndicators[i].SetPropertyBlock(color);
                if (gateLights != null && i < gateLights.Length && gateLights[i] != null)
                {
                    gateLights[i].color = lit ? Color.cyan : new Color(1f, .55f, .2f);
                    gateLights[i].intensity = lit ? 1.5f : .15f;
                }
            }
            if (broken && both && !door.IsOpen)
            {
                door.Open();
                if (solveSound != null)
                    solveSound.Play();
            }
            if (door.IsOpen && exitBarrier != null && exitBarrier.activeSelf)
                exitBarrier.SetActive(false);
        }
    }
}
