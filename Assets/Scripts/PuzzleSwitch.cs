using UnityEngine;

namespace Drift
{
    public sealed class PuzzleSwitch : Interactable
    {
        [SerializeField]
        private PuzzleDoor door;
        public override string Prompt => "E — ACTIVATE SWITCH";
        public override bool CanInteract => base.CanInteract && door != null && !door.IsOpen;

        public override void Interact(PlayerInteraction player)
        {
            if (!CanInteract)
                return;
            door.Open();
            player.ShowMessage("Door unlocked", 2f);
        }
    }
}
