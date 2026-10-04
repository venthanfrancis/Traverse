using UnityEngine;
using UnityEngine.InputSystem;

namespace Drift
{
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField, Min(0.1f)]
        private float interactionRadius = 2.5f;
        [SerializeField]
        private LayerMask interactionLayers = ~0;
        private Interactable nearby;
        private string message;
        private float messageUntil;
        private GUIStyle promptStyle;
        public bool InputEnabled { get; set; } = true;
        public Interactable Nearby => nearby;

        private void Update()
        {
            nearby = null;
            if (!InputEnabled)
                return;
            Vector3 origin = transform.position + Vector3.up;
            float best = float.PositiveInfinity;
            foreach (Collider collider in Physics.OverlapSphere(origin, interactionRadius, interactionLayers, QueryTriggerInteraction.Collide))
            {
                Interactable item = collider.GetComponentInParent<Interactable>();
                if (item == null || !item.CanInteract)
                    continue;
                Vector3 point = collider is MeshCollider mesh && !mesh.convex ? collider.bounds.ClosestPoint(origin) : collider.ClosestPoint(origin);
                float distance = (point - origin).sqrMagnitude;
                if (distance >= best)
                    continue;
                // A wall between player and artifact should prevent interacting through it.
                Vector3 end = collider.bounds.center;
                bool blocked = false;
                foreach (RaycastHit hit in Physics.RaycastAll(origin, (end - origin).normalized, Vector3.Distance(origin, end), ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(item.transform))
                        continue;
                    blocked = true;
                    break;
                }

                if (blocked)
                    continue;
                best = distance;
                nearby = item;
            }

            if (nearby != null && Application.isFocused && Cursor.lockState == CursorLockMode.Locked && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                nearby.Interact(this);
        }

        public void ShowMessage(string text, float seconds = 2f)
        {
            message = text;
            messageUntil = Time.unscaledTime + seconds;
        }

        private void OnGUI()
        {
            if (!InputEnabled || Time.timeScale <= 0f)
                return;
            string text = Time.unscaledTime < messageUntil ? message : nearby != null ? nearby.Prompt : null;
            if (string.IsNullOrEmpty(text))
                return;
            if (promptStyle == null) promptStyle = GameUI.Text(12);
            Matrix4x4 previous = GameUI.Begin();
            var panel = new Rect(350f, 486f, 260f, 34f);
            GameUI.Label(new Rect(panel.x + 10f, panel.y + 4f, panel.width - 20f, panel.height - 8f), text, promptStyle);
            GUI.matrix = previous;
        }
    }
}
