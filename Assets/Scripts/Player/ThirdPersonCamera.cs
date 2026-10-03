using UnityEngine;
using UnityEngine.InputSystem;

namespace Drift
{
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField]
        private Transform cameraTarget;
        [SerializeField]
        private InputActionAsset inputActions;
        [SerializeField, Min(0f)]
        private float mouseSensitivity = 0.12f;
        [SerializeField, Min(0.1f)]
        private float cameraDistance = 4f;
        [SerializeField]
        private Vector3 targetOffset = Vector3.zero;
        [SerializeField]
        private float minimumPitch = -35f;
        [SerializeField]
        private float maximumPitch = 70f;
        [SerializeField]
        private float startingPitch = 15f;
        [SerializeField, Min(0.001f)]
        private float followSmoothTime = 0.06f;
        [SerializeField, Min(0.001f)]
        private float rotationSmoothTime = 0.04f;
        [SerializeField, Min(0.001f)]
        private float distanceSmoothTime = 0.15f;
        [Tooltip("Exclude the Player layer; include walls, ground, and platforms.")]
        [SerializeField]
        private LayerMask collisionLayers = ~0;
        [SerializeField, Min(0.01f)]
        private float collisionRadius = 0.25f;
        [SerializeField, Min(0f)]
        private float collisionPadding = 0.05f;
        private InputActionAsset runtimeActions;
        private InputAction look;
        private Transform playerRoot;
        private Vector3 pivot, pivotVelocity;
        private float yaw, pitch, smoothYaw, smoothPitch, yawVelocity, pitchVelocity;
        private float distance, distanceVelocity;
        private bool skipLook;
        public float MouseSensitivity { get => mouseSensitivity; set => mouseSensitivity = Mathf.Clamp(value, 0.02f, 0.5f); }

        // Recenter after an explicit teleport without smoothing across the whole level.
        public void SnapToTarget(float heading)
        {
            pivot = cameraTarget.position + targetOffset;
            pivotVelocity = Vector3.zero;
            yaw = smoothYaw = heading;
            pitch = smoothPitch = Mathf.Clamp(startingPitch, minimumPitch, maximumPitch);
            yawVelocity = pitchVelocity = distanceVelocity = 0f;
            distance = cameraDistance;
            Quaternion rotation = Quaternion.Euler(smoothPitch, smoothYaw, 0f);
            transform.SetPositionAndRotation(pivot + rotation * Vector3.back * distance, rotation);
            skipLook = true;
        }

        private void Awake()
        {
            if (cameraTarget == null || inputActions == null)
            {
                Debug.LogError("Assign Camera Target and Input Actions to ThirdPersonCamera.", this);
                enabled = false;
                return;
            }

            runtimeActions = Instantiate(inputActions);
            CharacterController playerController = cameraTarget.GetComponentInParent<CharacterController>();
            playerRoot = playerController != null ? playerController.transform : cameraTarget.parent;
            look = runtimeActions.FindAction("Player/Look", true);
            // The prototype uses mouse delta, not the asset's optional stick/touch bindings.
            look.bindingMask = InputBinding.MaskByGroup("Keyboard&Mouse");
            pivot = cameraTarget.position + targetOffset;
            yaw = smoothYaw = transform.eulerAngles.y;
            pitch = smoothPitch = Mathf.Clamp(startingPitch, minimumPitch, maximumPitch);
            distance = cameraDistance;
        }

        private void OnEnable()
        {
            look?.Enable();
            if (look != null)
                SetCursor(true);
        }

        private void OnDisable()
        {
            look?.Disable();
            SetCursor(false);
        }

        private void OnDestroy()
        {
            if (runtimeActions != null)
                Destroy(runtimeActions);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                SetCursor(false);
        }

        private void SetCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
            skipLook = true;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SetCursor(false);
            else if (Application.isFocused && Cursor.lockState != CursorLockMode.Locked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                SetCursor(true);
            if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked)
                return;
            if (skipLook)
            {
                skipLook = false;
                return;
            }

            Vector2 delta = look.ReadValue<Vector2>();
            // Mouse delta is already displacement per frame: do not multiply by deltaTime.
            yaw = Mathf.Repeat(yaw + delta.x * mouseSensitivity, 360f);
            pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, minimumPitch, maximumPitch);
        }

        private void LateUpdate()
        {
            if (Time.deltaTime <= 0f)
                return;
            pivot = Vector3.SmoothDamp(pivot, cameraTarget.position + targetOffset, ref pivotVelocity, followSmoothTime);
            smoothYaw = Mathf.SmoothDampAngle(smoothYaw, yaw, ref yawVelocity, rotationSmoothTime);
            smoothPitch = Mathf.SmoothDampAngle(smoothPitch, pitch, ref pitchVelocity, rotationSmoothTime);
            Quaternion rotation = Quaternion.Euler(smoothPitch, smoothYaw, 0f);
            Vector3 backwards = rotation * Vector3.back;
            float allowed = cameraDistance;
            // A smoothed pivot can lag behind the moving player. Ignore the entire player
            // hierarchy explicitly, even if its layer was accidentally included in the mask.
            // Inspect every hit so ignoring the player does not hide a wall behind it.
            RaycastHit[] hits = Physics.SphereCastAll(pivot, collisionRadius, backwards, cameraDistance, collisionLayers, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (playerRoot != null && hit.collider.transform.IsChildOf(playerRoot))
                    continue;
                allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - collisionPadding));
            }

            // Pull inward immediately; smooth only the return so smoothing cannot cross a wall.
            if (allowed < distance)
            {
                distance = allowed;
                distanceVelocity = 0f;
            }
            else
                distance = Mathf.Min(allowed, Mathf.SmoothDamp(distance, allowed, ref distanceVelocity, distanceSmoothTime));
            transform.SetPositionAndRotation(pivot + backwards * distance, rotation);
        }

        private void OnValidate()
        {
            minimumPitch = Mathf.Clamp(minimumPitch, -89f, 89f);
            maximumPitch = Mathf.Clamp(maximumPitch, minimumPitch, 89f);
        }
    }
}
