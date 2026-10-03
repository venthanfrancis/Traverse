using UnityEngine;

namespace Drift
{
    [DefaultExecutionOrder(250)]
    public sealed class CameraShake : MonoBehaviour
    {
        [SerializeField]
        private ThirdPersonCamera orbit;
        private float remaining, length, strength;
        private Vector3 applied;
        private void Update()
        {
            if (applied != Vector3.zero)
            {
                transform.position -= applied;
                applied = Vector3.zero;
            }
        }

        public void Shake(float duration, float intensity)
        {
            length = remaining = Mathf.Max(remaining, duration);
            strength = Mathf.Max(strength, Mathf.Clamp(intensity, 0f, 0.08f));
        }

        private void LateUpdate()
        {
            if (Time.deltaTime <= 0f || remaining <= 0f)
                return;
            remaining = Mathf.Max(0f, remaining - Time.deltaTime);
            float weight = remaining / Mathf.Max(length, .001f);
            applied = new Vector3(Mathf.Sin(Time.time * 47f), Mathf.Cos(Time.time * 39f), 0) * strength * weight;
            transform.position += applied;
            if (remaining <= 0f)
                strength = 0f;
        }

        private void OnDisable()
        {
            transform.position -= applied;
            applied = Vector3.zero;
            remaining = strength = 0f;
        }

        public void Clear()
        {
            transform.position -= applied;
            applied = Vector3.zero;
            remaining = strength = 0f;
        }
    }
}
