using UnityEngine;

namespace Drift
{
    public abstract class Interactable : MonoBehaviour
    {
        public virtual string Prompt => "E — INTERACT";
        public virtual bool CanInteract => isActiveAndEnabled;

        public abstract void Interact(PlayerInteraction player);
    }
}
