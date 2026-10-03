using UnityEngine;

namespace Drift
{
    // Shared look for the code-drawn HUD. All sizes are written for a 1080p screen
    // ("design pixels") and grow or shrink with the real screen height.
    public static class Hud
    {
        private static GUIStyle label;
        private static readonly Color PanelColor = new Color(.025f, .035f, .055f, .88f);
        private static readonly Color AccentColor = new Color(.2f, .8f, 1f, 1f);

        public static float Scale => Mathf.Clamp(Screen.height / 1080f, .75f, 3f);

        public static float Px(float designPixels) => designPixels * Scale;

        // Respects the alpha of GUI.color, so callers can fade a panel by setting it first.
        public static void Panel(Rect rect)
        {
            Color previous = GUI.color;
            GUI.color = new Color(PanelColor.r, PanelColor.g, PanelColor.b, PanelColor.a * previous.a);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, previous.a);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, Mathf.Max(2f, Px(3f))), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        public static void Label(Rect rect, string text, float designFontSize, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            if (label == null)
                label = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    wordWrap = true
                };
            label.fontSize = Mathf.RoundToInt(Px(designFontSize));
            label.alignment = alignment;
            GUI.Label(rect, text, label);
        }

        public static void Box(Rect rect, string text, float designFontSize)
        {
            Panel(rect);
            Label(rect, text, designFontSize);
        }
    }
}
