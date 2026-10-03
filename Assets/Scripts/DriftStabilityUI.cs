using UnityEngine;

namespace Drift
{
    // Removable prototype overlay: no Canvas, UI package, or input handling needed.
    public sealed class DriftStabilityUI : MonoBehaviour
    {
        [SerializeField]
        private RealityManager realityManager;
        private float opacity, lastStability, lastRemaining;
        private void Update()
        {
            if (realityManager == null)
                return;
            opacity = Mathf.MoveTowards(opacity, realityManager.IsDrifting ? 1f : 0f, Time.deltaTime * 6f);
            if (realityManager.IsDrifting)
            {
                lastStability = realityManager.Stability;
                lastRemaining = realityManager.RemainingDriftTime;
            }

            if (!realityManager.GameplayInputEnabled)
                opacity = 0f;
            if (!realityManager.IsDrifting && realityManager.RemainingDriftTime > 0f)
                opacity = 0f; // respawn hides immediately
        }

        private void Awake()
        {
            if (realityManager == null)
                realityManager = GetComponent<RealityManager>();
            if (realityManager == null)
            {
                Debug.LogError("Assign Reality Manager to DriftStabilityUI.", this);
                enabled = false;
            }
        }

        private void OnGUI()
        {
            if (Time.timeScale <= 0f || realityManager == null || opacity <= 0f)
                return;
            float remaining = lastRemaining;
            bool critical = remaining <= 3f;
            bool warning = remaining <= 5f;
            string title = "DRIFT STABILITY";
            // Top right, so it never overlaps the objective panel (top left).
            float width = Mathf.Min(Hud.Px(560f), Screen.width * 0.45f);
            float left = Screen.width - width - Hud.Px(24f);
            float pad = Hud.Px(20f);
            Color previous = GUI.color;
            float top = Hud.Px(24f);
            GUI.color = new Color(1, 1, 1, opacity);
            Hud.Panel(new Rect(left, top, width, Hud.Px(116f)));
            Hud.Label(new Rect(left, top + Hud.Px(8f), width, Hud.Px(36f)), title, 28f);
            GUI.color = new Color(.12f, .16f, .18f, opacity);
            GUI.DrawTexture(new Rect(left + pad, top + Hud.Px(52f), width - pad * 2f, Hud.Px(12f)), Texture2D.whiteTexture);
            Color bar = critical ? Color.red : warning ? new Color(1f, 0.65f, 0.1f) : Color.cyan;
            if (warning)
                bar *= 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * (critical ? 12f : 5f)));
            bar.a = opacity;
            GUI.color = bar;
            GUI.DrawTexture(new Rect(left + pad, top + Hud.Px(52f), (width - pad * 2f) * lastStability, Hud.Px(12f)), Texture2D.whiteTexture);
            GUI.color = new Color(1, 1, 1, opacity);
            Hud.Label(new Rect(left, top + Hud.Px(70f), width, Hud.Px(36f)), $"{remaining:0.0}s", 30f);
            GUI.color = previous;
        }
    }
}
