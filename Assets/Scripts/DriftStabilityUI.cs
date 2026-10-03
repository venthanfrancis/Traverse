using UnityEngine;

namespace Drift
{
    // Removable prototype overlay: no Canvas, UI package, or input handling needed.
    public sealed class DriftStabilityUI : MonoBehaviour
    {
        [SerializeField]
        private RealityManager realityManager;
        private GUIStyle labelStyle;
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
            if (labelStyle == null)
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 18
                };
            float remaining = lastRemaining;
            bool critical = remaining <= 3f;
            bool warning = remaining <= 5f;
            string title = "DRIFT STABILITY";
            float width = Mathf.Min(360f, Screen.width - 24f);
            float left = (Screen.width - width) * 0.5f;
            Color previous = GUI.color;
            float top = Screen.height - 155f;
            GUI.color = new Color(.025f, .035f, .055f, opacity * .85f);
            GUI.DrawTexture(new Rect(left, top, width, 64f), Texture2D.whiteTexture);
            GUI.color = new Color(1, 1, 1, opacity);
            GUI.Label(new Rect(left, top + 2f, width, 24f), title, labelStyle);
            GUI.color = new Color(.12f, .16f, .18f, opacity);
            GUI.DrawTexture(new Rect(left + 12f, top + 30f, width - 24f, 5f), Texture2D.whiteTexture);
            Color bar = critical ? Color.red : warning ? new Color(1f, 0.65f, 0.1f) : Color.cyan;
            if (warning)
                bar *= 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * (critical ? 12f : 5f)));
            bar.a = opacity;
            GUI.color = bar;
            GUI.DrawTexture(new Rect(left + 12f, top + 30f, (width - 24f) * lastStability, 5f), Texture2D.whiteTexture);
            GUI.color = new Color(1, 1, 1, opacity);
            GUI.Label(new Rect(left, top + 37f, width, 24f), $"{remaining:0.0}s", labelStyle);
            GUI.color = previous;
        }
    }
}
