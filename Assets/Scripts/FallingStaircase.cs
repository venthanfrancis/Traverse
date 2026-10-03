using UnityEngine;

namespace Drift
{
    // The landing belongs to Broken World. Its seal references stay within this scene.
    public sealed class FallingStaircase : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] seals;
        public bool IsSolved { get; private set; }

        private void OnTriggerStay(Collider other)
        {
            PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
            if (player == null || IsSolved || !player.GetComponent<CharacterController>().isGrounded)
                return;
            IsSolved = true;
            foreach (GameObject seal in seals)
                if (seal != null)
                    seal.SetActive(false);
        }
    }
}
