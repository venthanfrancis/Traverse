using UnityEngine;

namespace Drift
{
    public sealed class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private PlayerMovement player;
        private CharacterController controller;
        private string current;

        private void Awake()
        {
            player = GetComponent<PlayerMovement>();
            controller = GetComponent<CharacterController>();
        }

        private void LateUpdate()
        {
            if (animator == null || player == null || controller == null || Time.timeScale <= 0f) return;
            Vector3 velocity = controller.velocity;
            float speed = new Vector2(velocity.x, velocity.z).magnitude;
            string next = !player.enabled ? "Idle" : !player.IsGrounded ? velocity.y > .1f ? "Jump" : "Fall" : speed > 5f ? "Run" : speed > .2f ? "Walk" : "Idle";
            if (next != current)
            {
                current = next;
                animator.CrossFadeInFixedTime(next, .12f, 0);
            }
            animator.speed = next == "Walk" ? Mathf.Clamp(speed / 4f, .6f, 1.3f) : next == "Run" ? Mathf.Clamp(speed / 7f, .7f, 1.3f) : 1f;
        }
    }
}
