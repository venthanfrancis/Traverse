using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Drift
{
    // Prebuilt sky/profile references; no world instantiation or material creation on Q.
    public sealed class VisualAtmosphere : MonoBehaviour
    {
        [SerializeField]
        private GameFlowManager flow;
        [SerializeField]
        private OpeningSequence opening;
        [SerializeField]
        private RealityManager reality;
        [SerializeField]
        private Light keyLight;
        [SerializeField]
        private Material caveSky, brokenSky, normalSky;
        [SerializeField]
        private Volume volume;
        [SerializeField]
        private CameraShake shake;
        private Bloom bloom;
        private ColorAdjustments colors;
        private ChromaticAberration chromatic;
        private float pulse, collapseBeat;
        private Material originalSky;
        private Color originalAmbient, originalFog, originalKey;
        private bool originalFogEnabled;
        private float originalKeyIntensity;
        private float originalFogStart, originalFogEnd;
        private FogMode originalFogMode;
        private AmbientMode originalAmbientMode;
        private void Awake()
        {
            originalSky = RenderSettings.skybox;
            originalAmbient = RenderSettings.ambientLight;
            originalFog = RenderSettings.fogColor;
            originalFogEnabled = RenderSettings.fog;
            originalFogStart = RenderSettings.fogStartDistance;
            originalFogEnd = RenderSettings.fogEndDistance;
            originalFogMode = RenderSettings.fogMode;
            originalAmbientMode = RenderSettings.ambientMode;
            originalKey = keyLight.color;
            originalKeyIntensity = keyLight.intensity;
            if (volume != null)
            {
                volume.profile.TryGet(out bloom);
                volume.profile.TryGet(out colors);
                volume.profile.TryGet(out chromatic);
            }
        }

        private void OnEnable()
        {
            reality.DimensionChanged += Changed;
        }

        private void OnDisable()
        {
            reality.DimensionChanged -= Changed;
        }

        private void Changed(RealityManager.Dimension dimension)
        {
            if (reality.CanDrift)
                pulse = .55f;
        }

        private void Update()
        {
            bool cave = !opening.HasArrived || flow.Phase == GameFlowManager.GamePhase.Returning || flow.Phase == GameFlowManager.GamePhase.EndingWalk || flow.Phase == GameFlowManager.GamePhase.Ending;
            bool stable = !cave && reality.IsDrifting;
            bool escape = !cave && flow.Phase == GameFlowManager.GamePhase.FinalEscape;
            RenderSettings.skybox = cave ? caveSky : stable ? normalSky : brokenSky;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = cave ? new Color(.23f, .20f, .17f) : stable ? new Color(.43f, .48f, .46f) : new Color(.32f, .25f, .25f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = cave ? new Color(.09f, .08f, .07f) : stable ? new Color(.25f, .34f, .33f) : new Color(.16f, .025f, .024f);
            RenderSettings.fogStartDistance = cave ? 32f : 110f;
            RenderSettings.fogEndDistance = cave ? 120f : 390f;
            keyLight.color = cave ? new Color(1f, .81f, .59f) : stable ? new Color(.86f, 1f, .91f) : new Color(1f, .58f, .42f);
            keyLight.intensity = cave ? .55f : stable ? 1.15f : .8f;
            pulse = Mathf.Max(0f, pulse - Time.deltaTime);
            if (bloom != null)
                bloom.intensity.value = (cave ? .22f : stable ? .18f : .34f) + pulse * .3f;
            if (colors != null)
            {
                colors.saturation.value = stable ? -6f : cave ? -12f : -18f;
                colors.postExposure.value = stable ? .12f : 0f;
            }

            if (chromatic != null)
                chromatic.intensity.value = pulse * .08f;
            if (escape && !flow.IsPaused)
            {
                collapseBeat -= Time.deltaTime;
                if (collapseBeat <= 0)
                {
                    collapseBeat = 6f;
                    shake.Shake(.35f, .025f);
                }

                keyLight.intensity += Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Time.time * 2f)), 20f) * .25f;
            }

            if (cave && flow.Phase == GameFlowManager.GamePhase.EndingWalk)
                shake.Clear();
        }

        private void OnDestroy()
        {
            RenderSettings.skybox = originalSky;
            RenderSettings.ambientLight = originalAmbient;
            RenderSettings.fog = originalFogEnabled;
            RenderSettings.fogColor = originalFog;
            RenderSettings.fogStartDistance = originalFogStart;
            RenderSettings.fogEndDistance = originalFogEnd;
            RenderSettings.fogMode = originalFogMode;
            RenderSettings.ambientMode = originalAmbientMode;
            if (keyLight != null)
            {
                keyLight.color = originalKey;
                keyLight.intensity = originalKeyIntensity;
            }
        }
    }
}
