using UnityEngine;
using UnityEngine.InputSystem;

namespace Drift
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Transform movementCamera;
        [SerializeField]
        private InputActionAsset inputActions;
        [Header("Movement")]
        [SerializeField, Min(0f)]
        private float walkSpeed = 4f;
        [SerializeField, Min(0f)]
        private float sprintSpeed = 7f;
        [SerializeField, Min(0.001f)]
        private float rotationSmoothTime = 0.1f;
        [SerializeField, Min(0f)]
        private float jumpHeight = 1.5f;
        [SerializeField]
        private float gravity = -25f;
        [Tooltip("How quickly horizontal velocity changes in the air, in metres per second squared.")]
        [SerializeField, Min(0f)]
        private float airControl = 30f;
        [Header("Grounding")]
        [Tooltip("Exclude the Player layer. Include solid environment layers.")]
        [SerializeField]
        private LayerMask groundLayers = ~0;
        [SerializeField, Min(0f)]
        private float groundSnapDistance = 0.3f;
        private CharacterController controller;
        private InputActionAsset runtimeActions;
        private InputAction move, jump, sprint;
        private Vector3 horizontalVelocity;
        private float verticalVelocity, rotationVelocity;
        public bool IsGrounded { get; private set; }

        // Only for explicit spawn/teleport transitions; normal Drift never calls this.
        public void ResetMotion()
        {
            horizontalVelocity = Vector3.zero;
            verticalVelocity = rotationVelocity = 0f;
            IsGrounded = false;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (movementCamera == null && Camera.main != null)
                movementCamera = Camera.main.transform;
            if (inputActions == null || movementCamera == null)
            {
                Debug.LogError("Assign Input Actions and Movement Camera to PlayerMovement.", this);
                enabled = false;
                return;
            }

            // Own only a private copy of the actions used here; never enable unrelated gameplay.
            runtimeActions = Instantiate(inputActions);
            move = runtimeActions.FindAction("Player/Move", true);
            jump = runtimeActions.FindAction("Player/Jump", true);
            sprint = runtimeActions.FindAction("Player/Sprint", true);
        }

        private void OnEnable()
        {
            move?.Enable();
            jump?.Enable();
            sprint?.Enable();
        }

        private void OnDisable()
        {
            move?.Disable();
            jump?.Disable();
            sprint?.Disable();
        }

        private void OnDestroy()
        {
            if (runtimeActions != null)
                Destroy(runtimeActions);
        }

        private void Update()
        {
            if (!controller.enabled)
                return;
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;
            bool wasGrounded = IsGrounded;
            IsGrounded = verticalVelocity <= 0f && controller.isGrounded;
            Vector2 input = Application.isFocused && Cursor.lockState == CursorLockMode.Locked ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;
            // Camera right gives a stable yaw basis even when looking nearly straight up/down.
            Vector3 right = Vector3.ProjectOnPlane(movementCamera.right, Vector3.up).normalized;
            Vector3 forward = Vector3.Cross(right, Vector3.up);
            Vector3 direction = right * input.x + forward * input.y;
            Vector3 desired = direction * (sprint.IsPressed() ? sprintSpeed : walkSpeed);
            horizontalVelocity = IsGrounded ? desired : Vector3.MoveTowards(horizontalVelocity, desired, airControl * dt);
            // Releasing input stops horizontal travel immediately, including in the air.
            if (input.sqrMagnitude < 0.0001f)
                horizontalVelocity = Vector3.zero;
            if (direction.sqrMagnitude > 0.0001f)
            {
                float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, Mathf.SmoothDampAngle(transform.eulerAngles.y, yaw, ref rotationVelocity, rotationSmoothTime), 0f);
            }

            bool jumped = IsGrounded && inputAllowed() && jump.WasPressedThisFrame();
            if (IsGrounded)
                verticalVelocity = -2f;
            if (jumped)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                IsGrounded = false;
            }

            verticalVelocity += gravity * dt;
            // Disable step climbing during ascent so stairs cannot lift an airborne character.
            float step = controller.stepOffset;
            controller.stepOffset = IsGrounded ? step : 0f;
            CollisionFlags flags = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            controller.stepOffset = step;
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
                verticalVelocity = 0f;
            IsGrounded = verticalVelocity <= 0f && (flags & CollisionFlags.Below) != 0;
            // Snap only from a grounded frame, never after a jump or while falling from a ledge.
            if (!jumped && wasGrounded && !IsGrounded && verticalVelocity <= 0f && TryGroundDistance(out float distance))
            {
                flags = controller.Move(Vector3.down * (distance + 0.02f));
                IsGrounded = (flags & CollisionFlags.Below) != 0;
            }

            if (IsGrounded)
                verticalVelocity = -2f;
        }

        private bool inputAllowed() => Application.isFocused && Cursor.lockState == CursorLockMode.Locked;
        private bool TryGroundDistance(out float distance)
        {
            Vector3 center = transform.TransformPoint(controller.center);
            float radius = controller.radius * 0.9f;
            Vector3 origin = center - Vector3.up * (controller.height * 0.5f - controller.radius);
            float inset = controller.radius - radius;
            if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, groundSnapDistance + inset, groundLayers, QueryTriggerInteraction.Ignore) && Vector3.Angle(hit.normal, Vector3.up) <= controller.slopeLimit)
            {
                distance = Mathf.Max(0f, hit.distance - inset);
                return true;
            }

            distance = 0f;
            return false;
        }

        private void OnValidate()
        {
            gravity = Mathf.Min(gravity, -0.01f);
            rotationSmoothTime = Mathf.Max(0.001f, rotationSmoothTime);
        }
    }
}
