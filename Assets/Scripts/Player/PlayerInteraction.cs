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
                float distance = (collider.ClosestPoint(origin) - origin).sqrMagnitude;
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
            if (!InputEnabled)
                return;
            string text = Time.unscaledTime < messageUntil ? message : nearby != null ? nearby.Prompt : null;
            if (string.IsNullOrEmpty(text))
                return;
            GUI.Box(new Rect(Screen.width * 0.5f - 160f, Screen.height - 90f, 320f, 40f), text);
        }
    }
}
