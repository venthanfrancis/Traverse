using UnityEngine;

namespace Drift
{
    [RequireComponent(typeof(PlayerMovement), typeof(CharacterController))]
    public sealed class PlayerRespawn : MonoBehaviour
    {
        [SerializeField]
        private RealityManager realityManager;
        [SerializeField]
        private ThirdPersonCamera playerCamera;
        [SerializeField]
        private Transform initialCheckpoint;
        [SerializeField]
        private float fallHeight = -10f;
        private PlayerMovement movement;
        public Transform CurrentCheckpoint { get; private set; }

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            CurrentCheckpoint = initialCheckpoint;
            if (realityManager == null || playerCamera == null)
            {
                Debug.LogError("Assign manager, camera and initial checkpoint to PlayerRespawn.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            // The opening owns its explicit transition. Respawn starts after ability unlock.
            if (realityManager.CanDrift && movement.enabled && transform.position.y < fallHeight)
                Respawn();
        }

        public void SetCheckpoint(Transform checkpoint)
        {
            if (checkpoint != null)
                CurrentCheckpoint = checkpoint;
        }

        public void Respawn()
        {
            if (CurrentCheckpoint == null || !realityManager.CanDrift)
                return;
            foreach (var block in FindObjectsByType<EchoBlockPuzzle>(FindObjectsSortMode.None))
                block.ReleaseCarry();
            realityManager.ResetForRespawn();
            CharacterController controller = GetComponent<CharacterController>();
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.SetPositionAndRotation(CurrentCheckpoint.position, CurrentCheckpoint.rotation);
            movement.ResetMotion();
            controller.enabled = wasEnabled;
            Physics.SyncTransforms();
            playerCamera.SnapToTarget(CurrentCheckpoint.eulerAngles.y);
            GetComponent<PlayerInteraction>()?.ShowMessage("Returned to checkpoint", 2f);
        }
    }
}
