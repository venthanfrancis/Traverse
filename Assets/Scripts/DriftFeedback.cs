using UnityEngine;

namespace Drift
{
    [DefaultExecutionOrder(100)]
    public sealed class DriftFeedback : MonoBehaviour
    {
        [SerializeField]
        private RealityManager realityManager;
        [SerializeField]
        private GameFlowManager gameFlow;
        [SerializeField]
        private GameAudioManager audioManager;
        [SerializeField]
        private ParticleSystem transitionMotes;
        private float nextWarning;

        private void OnEnable()
        {
            if (transitionMotes != null)
                transitionMotes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            realityManager.DimensionChanged += Changed;
            realityManager.BeforeReturnToBroken += Returning;
        }

        private void OnDisable()
        {
            realityManager.DimensionChanged -= Changed;
            realityManager.BeforeReturnToBroken -= Returning;
        }

        private void Changed(RealityManager.Dimension dimension)
        {
            if (!realityManager.CanDrift || !gameFlow.IsGameplayRunning)
                return;
            bool forced = dimension == RealityManager.Dimension.Broken && realityManager.RemainingDriftTime <= 0f;
            // The pre-return event distinguishes expiry from an early manual return.
            forced = forced && expired;
            nextWarning = 5f;
            audioManager.PlayDrift(dimension == RealityManager.Dimension.Normal, forced);
            expired = false;
        }

        private void Update()
        {
            if (!gameFlow.IsGameplayRunning)
                return;
            if (realityManager.IsDrifting && realityManager.RemainingDriftTime <= nextWarning)
            {
                audioManager.PlayWarning();
                nextWarning = realityManager.RemainingDriftTime > 3f ? nextWarning - 1f : nextWarning - .5f;
            }
        }

        private bool expired;
        private void Returning()
        {
            expired = realityManager.RemainingDriftTime <= .01f;
        }
    }
}
