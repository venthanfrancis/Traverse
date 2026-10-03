using UnityEngine;

namespace Drift
{
    public sealed class FloatingObject : MonoBehaviour
    {
        [SerializeField]
        private float speed = 0.7f, distance = 0.25f;
        [SerializeField]
        private Vector3 rotationSpeed = new Vector3(0f, 7f, 0f);
        private Vector3 origin;
        private Quaternion rotation;
        private float phase, age;
        private void Awake()
        {
            origin = transform.localPosition;
            rotation = transform.localRotation;
            phase = Random.value * 6.28f;
        }

        private void Update()
        {
            age += Time.deltaTime;
            transform.localPosition = origin + Vector3.up * Mathf.Sin(age * speed + phase) * distance;
            transform.localRotation = rotation * Quaternion.Euler(rotationSpeed * age);
        }
    }
}
