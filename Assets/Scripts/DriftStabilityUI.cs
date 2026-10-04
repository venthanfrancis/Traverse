using UnityEngine;
using UnityEngine.SceneManagement;

namespace Drift
{
    public sealed class DriftStabilityUI : MonoBehaviour
    {
        [SerializeField] private RealityManager realityManager;
        private GameFlowManager flow;
        private GUIStyle heading, caption, timer;

        private void Awake()
        {
            if (realityManager == null) realityManager = GetComponent<RealityManager>();
            flow = GetComponent<GameFlowManager>();
        }

        private void OnGUI()
        {
            if (realityManager == null || flow == null || !DriftSceneLoader.IsReady || SceneManager.GetActiveScene().name != "BrokenWorld" || flow.Phase == GameFlowManager.GamePhase.Title || flow.Phase == GameFlowManager.GamePhase.Ending || flow.IsPaused) return;
            if (heading == null)
            {
                heading = GameUI.Text(10, TextAnchor.MiddleLeft);
                caption = GameUI.Text(10, TextAnchor.MiddleLeft);
                caption.normal.textColor = GameUI.Muted;
                timer = GameUI.Text(14, TextAnchor.MiddleRight);
            }
            Matrix4x4 previous = GameUI.Begin();
            float remaining = realityManager.RemainingDriftTime;
            bool unlocked = realityManager.CanDrift;
            bool draining = realityManager.IsDrifting;
            bool low = draining && remaining <= 3f;
            Color accent = low ? new Color(1f, .48f, .30f) : GameUI.Accent;
            GameUI.Label(new Rect(30f, 24f, 136f, 18f), "STABILITY", heading);
            GameUI.Label(new Rect(168f, 24f, 46f, 18f), unlocked ? $"{remaining:0.0}s" : "--", timer);
            GameUI.Fill(new Rect(30f, 45f, 184f, 3f), new Color(.14f, .21f, .23f));
            GameUI.Fill(new Rect(30f, 45f, 184f * realityManager.Stability, 3f), unlocked ? accent : GameUI.Muted);
            string status = !unlocked ? "LOCKED" : draining ? "DRAINING" : realityManager.Stability >= .999f ? "READY" : "RECHARGING";
            GameUI.Label(new Rect(30f, 51f, 124f, 16f), status, caption);
            if (unlocked)
                GameUI.Label(new Rect(166f, 51f, 48f, 16f), realityManager.CanEnterDrift || draining ? "Q" : "WAIT", timer);
            GUI.matrix = previous;
        }
    }
}
