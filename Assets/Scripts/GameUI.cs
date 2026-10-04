using UnityEngine;

namespace Drift
{
    public static class GameUI
    {
        public static readonly Color Accent = new Color(.78f, .84f, .82f);
        public static readonly Color PanelColor = new Color(.035f, .055f, .065f, .94f);
        public static readonly Color Muted = new Color(.64f, .73f, .76f);
        private static GUIStyle button, toggle, slider, thumb;

        public static Matrix4x4 Begin()
        {
            Matrix4x4 previous = GUI.matrix;
            float scale = Mathf.Max(.01f, Mathf.Min(Screen.width / 960f, Screen.height / 540f));
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 960f * scale) * .5f, (Screen.height - 540f * scale) * .5f), Quaternion.identity, Vector3.one * scale);
            return previous;
        }

        public static GUIStyle Text(int size, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            return new GUIStyle(GUI.skin.label) { fontSize = size, alignment = alignment, wordWrap = true, richText = false, normal = { textColor = new Color(.92f, .96f, .97f) } };
        }

        public static void Fill(Rect area, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        public static void Panel(Rect area)
        {
            Fill(area, PanelColor);
        }

        public static void Label(Rect area, string text, GUIStyle style)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, previous.a * .85f);
            GUI.Label(new Rect(area.x + 1f, area.y + 1f, area.width, area.height), text, style);
            GUI.color = previous;
            GUI.Label(area, text, style);
        }

        private static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static void PrepareControls()
        {
            if (button != null) return;
            var idle = Solid(new Color(1f, 1f, 1f, 0f));
            var hover = Solid(new Color(1f, 1f, 1f, .07f));
            var active = Solid(new Color(1f, 1f, 1f, .12f));
            button = Text(14);
            button.padding = new RectOffset(14, 14, 8, 8);
            button.margin = new RectOffset(0, 0, 0, 8);
            button.normal.background = idle;
            button.hover.background = hover;
            button.active.background = active;
            button.hover.textColor = Color.white;
            button.active.textColor = Accent;
            toggle = new GUIStyle(button) { alignment = TextAnchor.MiddleLeft };
            toggle.onNormal.background = hover;
            toggle.onHover.background = active;
            toggle.onActive.background = active;
            slider = new GUIStyle(GUI.skin.horizontalSlider);
            slider.normal.background = Solid(new Color(.25f, .28f, .28f));
            slider.fixedHeight = 3f;
            slider.margin = new RectOffset(0, 0, 10, 10);
            thumb = new GUIStyle(GUI.skin.horizontalSliderThumb);
            thumb.normal.background = Solid(Accent);
            thumb.hover.background = thumb.normal.background;
            thumb.active.background = thumb.normal.background;
            thumb.fixedWidth = 12f;
            thumb.fixedHeight = 16f;
        }

        public static bool Button(string label)
        {
            PrepareControls();
            return GUILayout.Button(label, button, GUILayout.Height(34f));
        }

        public static bool Toggle(string label, bool value)
        {
            PrepareControls();
            return GUILayout.Toggle(value, (value ? "ON   " : "OFF   ") + label, toggle, GUILayout.Height(32f));
        }

        public static float Slider(float value, float min, float max)
        {
            PrepareControls();
            return GUILayout.HorizontalSlider(value, min, max, slider, thumb);
        }
    }
}
