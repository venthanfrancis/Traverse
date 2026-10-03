using UnityEngine;

namespace Drift
{
    public sealed class CollapseAtmosphere : MonoBehaviour
    {
        [SerializeField]
        private AnchorController anchor;
        [SerializeField]
        private Transform[] debris;
        [SerializeField]
        private Light flickerLight;
        private Vector3[] positions;
        private void Awake()
        {
            positions = new Vector3[debris.Length];
            for (int i = 0; i < debris.Length; i++)
                positions[i] = debris[i].localPosition;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
                return;
            float strength = anchor != null && anchor.IsRestored ? 1.5f : 0.4f;
            for (int i = 0; i < debris.Length; i++)
            {
                debris[i].localPosition = positions[i] + Vector3.up * Mathf.Sin(Time.time * 1.5f + i) * strength;
                debris[i].Rotate(Vector3.up, 12f * strength * Time.deltaTime);
            }

            if (flickerLight != null)
                flickerLight.intensity = 3f + Mathf.Sin(Time.time * 17f) * strength;
        }
    }
}
