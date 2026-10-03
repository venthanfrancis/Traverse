using UnityEngine;

namespace Drift
{
    public sealed class EndingExit : MonoBehaviour
    {
        [SerializeField]
        private GameFlowManager gameFlow;
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerMovement>() != null)
                gameFlow.FinishEnding();
        }
    }
}
