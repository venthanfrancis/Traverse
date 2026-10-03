using UnityEngine;
using Beat = NarrationSequence.NarrationBeats;

namespace Drift
{
    public sealed class ObservatoryTelescope : Interactable
    {
        [SerializeField]
        private GameObject[] shortcutSeals;
        [SerializeField]
        private Transform telescope;
        [SerializeField]
        private Vector3 destination = new Vector3(168, 19, 111);
        [SerializeField]
        private AudioClip discoverySound;
        public bool Activated { get; private set; }
        public override string Prompt => "E — ALIGN";
        public override bool CanInteract => base.CanInteract && !Activated;

        public override void Interact(PlayerInteraction player)
        {
            if (!CanInteract)
                return;
            Activated = true;
            telescope.rotation = Quaternion.LookRotation(destination - telescope.position);
            foreach (GameObject seal in shortcutSeals)
                if (seal != null)
                    seal.SetActive(false);
            if (discoverySound != null)
                GetComponent<AudioSource>().PlayOneShot(discoverySound, .5f);
            NarrationManager.Announce(Beat.TelescopeAligned);
        }
    }
}
