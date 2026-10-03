using System.Collections;
using UnityEngine;

namespace Drift
{
    public sealed class ScreenFade : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup overlay;
        private void Awake()
        {
            if (overlay == null)
            {
                Debug.LogError("Assign the fade CanvasGroup.", this);
                enabled = false;
                return;
            }

            SetAlpha(0f);
        }

        public IEnumerator FadeOut(float seconds) => FadeTo(1f, seconds);
        public IEnumerator FadeIn(float seconds) => FadeTo(0f, seconds);
        private IEnumerator FadeTo(float target, float seconds)
        {
            float from = overlay.alpha;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(from, target, Mathf.Clamp01(elapsed / seconds)));
                yield return null;
            }

            SetAlpha(target);
        }

        private void SetAlpha(float alpha)
        {
            overlay.alpha = alpha;
            overlay.blocksRaycasts = alpha > 0f;
        }
    }
}
