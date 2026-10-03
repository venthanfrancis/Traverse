using UnityEngine;

namespace Drift
{
    // Keep this state object outside the world roots so activation survives reality changes.
    public sealed class PuzzleDoor : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] barriers;
        public bool IsOpen { get; private set; }

        public void Open()
        {
            if (IsOpen)
                return;
            IsOpen = true;
            foreach (GameObject barrier in barriers)
                if (barrier != null)
                    barrier.SetActive(false);
            Physics.SyncTransforms();
        }
    }
}
