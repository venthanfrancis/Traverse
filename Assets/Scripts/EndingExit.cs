using UnityEngine;

namespace Drift
{
    public sealed class EndingExit : MonoBehaviour
    {
        [SerializeField]
        private GameFlowManager gameFlow;
        [SerializeField] private GameObject openingBlocker;
        [SerializeField] private GameObject exitLight;
        private void Update()
        {
            if (gameFlow == null) return;
            bool returning = gameFlow.Phase == GameFlowManager.GamePhase.EndingWalk || gameFlow.Phase == GameFlowManager.GamePhase.Ending || gameFlow.Phase == GameFlowManager.GamePhase.Returning;
            if (openingBlocker != null && openingBlocker.activeSelf == returning) openingBlocker.SetActive(!returning);
            if (exitLight != null && exitLight.activeSelf != returning) exitLight.SetActive(returning);
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerMovement>() != null)
                gameFlow.FinishEnding();
        }
    }
}
