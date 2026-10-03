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
        private Camera gameplayCamera;
        [SerializeField]
        private ThirdPersonCamera cameraController;
        [SerializeField]
        private CameraShake cameraShake;
        [SerializeField]
        private ParticleSystem transitionMotes;
        private float flash, pulse, baseFov, nextWarning;
        private void Awake()
        {
            baseFov = gameplayCamera.fieldOfView;
        }

        private void OnEnable()
        {
            realityManager.DimensionChanged += Changed;
            realityManager.BeforeReturnToBroken += Returning;
        }

        private void OnDisable()
        {
            realityManager.DimensionChanged -= Changed;
            realityManager.BeforeReturnToBroken -= Returning;
            if (gameplayCamera != null)
                gameplayCamera.fieldOfView = baseFov;
        }

        private void Changed(RealityManager.Dimension dimension)
        {
            if (!realityManager.CanDrift || !gameFlow.IsGameplayRunning)
                return;
            bool forced = dimension == RealityManager.Dimension.Broken && realityManager.RemainingDriftTime <= 0f;
            // Manual returns also set the timer to zero; the pre-return event captures expiry.
            forced = forced && expired;
            flash = forced ? .16f : .08f;
            pulse = .55f;
            nextWarning = 5f;
            if (cameraShake != null)
                cameraShake.Shake(forced ? .4f : .25f, forced ? .045f : .018f);
            if (transitionMotes != null)
                transitionMotes.Play();
            audioManager.PlayDrift(dimension == RealityManager.Dimension.Normal, forced);
            expired = false;
        }

        private void Update()
        {
            if (!gameFlow.IsGameplayRunning)
                return;
            flash = Mathf.Max(0f, flash - Time.deltaTime);
            pulse = Mathf.Max(0f, pulse - Time.deltaTime);
            if (realityManager.IsDrifting && realityManager.RemainingDriftTime <= nextWarning)
            {
                audioManager.PlayWarning();
                nextWarning = realityManager.RemainingDriftTime > 3f ? nextWarning - 1f : nextWarning - .5f;
            }

            gameplayCamera.fieldOfView = baseFov + Mathf.Sin(pulse / .55f * Mathf.PI) * 2f;
        }

        private void LateUpdate()
        {
            if (cameraShake != null || !gameFlow.IsGameplayRunning || !cameraController.enabled || pulse <= 0f)
                return;
            // Orbit camera recomputes its position each frame, so this offset never accumulates.
            gameplayCamera.transform.position += new Vector3(Mathf.Sin(Time.time * 50f), Mathf.Cos(Time.time * 45f), 0f) * (pulse / 0.25f) * 0.025f;
        }

        private bool expired;
        private void Returning()
        {
            expired = realityManager.RemainingDriftTime <= .01f;
        }

        private void OnGUI()
        {
            if (!gameFlow.IsGameplayRunning)
                return;
            float warning = realityManager.IsDrifting && realityManager.RemainingDriftTime <= 2f ? 0.025f + 0.02f * Mathf.Abs(Mathf.Sin(Time.time * 9f)) : 0f;
            float alpha = Mathf.Max(flash, warning);
            if (alpha <= 0f)
                return;
            Color previous = GUI.color;
            GUI.color = new Color(0.4f, 0.75f, 1f, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
