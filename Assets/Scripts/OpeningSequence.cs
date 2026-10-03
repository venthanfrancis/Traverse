using System.Collections;
using UnityEngine;

namespace Drift
{
    public sealed class OpeningSequence : MonoBehaviour
    {
        [SerializeField]
        private RealityManager realityManager;
        [SerializeField]
        private GameObject openingCave;
        [SerializeField]
        private GameObject brokenWorld;
        [SerializeField]
        private GameObject normalWorld;
        [SerializeField]
        private PlayerMovement playerMovement;
        [SerializeField]
        private PlayerInteraction playerInteraction;
        [SerializeField]
        private ThirdPersonCamera playerCamera;
        [SerializeField]
        private Transform entranceSpawn;
        [SerializeField]
        private Transform brokenSpawn;
        [SerializeField]
        private ScreenFade screenFade;
        [SerializeField]
        private Light artifactLight;
        [SerializeField]
        private ParticleSystem artifactParticles;
        [SerializeField, Min(0f)]
        private float reactionTime = 1.1f;
        [SerializeField, Min(0.01f)]
        private float fadeTime = 0.65f;
        public bool IsTransitioning { get; private set; }
        public bool HasArrived { get; private set; }
        // The orb the player used, so only that orb reacts to the transition.
        public GameObject Initiator { get; private set; }

        private void Start()
        {
            if (realityManager == null || openingCave == null || brokenWorld == null || normalWorld == null || playerMovement == null || playerInteraction == null || playerCamera == null || entranceSpawn == null || brokenSpawn == null || screenFade == null)
            {
                Debug.LogError("OpeningSequence needs all world, player, spawn, and fade references assigned.", this);
                enabled = false;
                return;
            }

            realityManager.SetDriftUnlocked(false);
            brokenWorld.SetActive(false);
            normalWorld.SetActive(false);
            openingCave.SetActive(true);
            PlacePlayer(entranceSpawn);
        }

        public void BeginTransition()
        {
            BeginTransition(null, RealityManager.Dimension.Broken, null, null);
        }

        // Light and particles default to the corrupted orb's when null.
        public void BeginTransition(GameObject initiator, RealityManager.Dimension startIn, Light light, ParticleSystem particles)
        {
            if (!isActiveAndEnabled || HasArrived || IsTransitioning)
                return;
            Initiator = initiator;
            StartCoroutine(Transition(startIn, light != null ? light : artifactLight, particles != null ? particles : artifactParticles));
        }

        private IEnumerator Transition(RealityManager.Dimension startIn, Light light, ParticleSystem particles)
        {
            IsTransitioning = true;
            playerInteraction.InputEnabled = false;
            playerMovement.enabled = false;
            // Freeze the current framing during the reaction rather than accepting mouse input.
            playerCamera.enabled = false;
            if (light != null)
                playerCamera.transform.LookAt(light.transform.position);
            realityManager.SetDriftUnlocked(false);
            if (particles != null)
                particles.Play();
            float elapsed = 0f;
            while (elapsed < reactionTime)
            {
                elapsed += Time.unscaledDeltaTime;
                if (light != null)
                    light.intensity = 3f + Mathf.Abs(Mathf.Sin(elapsed * 25f)) * 7f;
                yield return null;
            }

            yield return screenFade.FadeOut(fadeTime);
            realityManager.ArriveInBrokenWorld();
            if (startIn == RealityManager.Dimension.Normal)
            {
                // Flip while the screen is still black. The normal world keeps its usual drift timer.
                realityManager.SetDriftUnlocked(true);
                realityManager.BeginDrift();
            }

            openingCave.SetActive(false);
            PlacePlayer(brokenSpawn);
            // Change the atmosphere while the screen is still fully black.
            HasArrived = true;
            yield return null;
            yield return screenFade.FadeIn(fadeTime);
            playerCamera.enabled = true;
            playerMovement.enabled = true;
            playerInteraction.InputEnabled = true;
            IsTransitioning = false;
            realityManager.SetDriftUnlocked(true);
        }

        private void PlacePlayer(Transform spawn)
        {
            CharacterController controller = playerMovement.GetComponent<CharacterController>();
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            playerMovement.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            playerMovement.ResetMotion();
            controller.enabled = wasEnabled;
            Physics.SyncTransforms();
            playerCamera.SnapToTarget(spawn.eulerAngles.y);
        }
    }
}
