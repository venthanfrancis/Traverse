using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Beat = NarrationSequence.NarrationBeats;

namespace Drift
{
    [DefaultExecutionOrder(-100)]
    public sealed class RealityManager : MonoBehaviour
    {
        public enum Dimension
        {
            Broken,
            Normal
        }

        [FormerlySerializedAs("normalWorld")]
        [SerializeField]
        private GameObject brokenWorld;
        [FormerlySerializedAs("driftWorld")]
        [InspectorName("Normal World")]
        [SerializeField]
        private GameObject stableWorld;
        [SerializeField, Min(0.01f)]
        private float driftDuration = 15f;
        [SerializeField]
        private bool driftUnlockedAtStart = true;
        public bool CanDrift { get; private set; }
        public bool GameplayInputEnabled { get; set; } = true;
        public Dimension CurrentDimension { get; private set; } = Dimension.Broken;
        public bool IsDrifting => CurrentDimension == Dimension.Normal;
        public float RemainingDriftTime { get; private set; }
        public float DriftDuration => driftDuration;
        public float Stability => IsDrifting ? Mathf.Clamp01(RemainingDriftTime / activeDuration) : 0f;

        public event Action BeforeReturnToBroken;
        public event Action<Dimension> DimensionChanged;
        private float activeDuration = 15f;
        private bool initialized;
        private void Awake()
        {
            if (brokenWorld == null || stableWorld == null || brokenWorld == stableWorld || brokenWorld.transform.IsChildOf(stableWorld.transform) || stableWorld.transform.IsChildOf(brokenWorld.transform) || transform.IsChildOf(brokenWorld.transform) || transform.IsChildOf(stableWorld.transform))
            {
                Debug.LogError("Assign separate Broken World and Normal World roots. Keep GameManager outside both.", this);
                enabled = false;
                return;
            }

            initialized = true;
            CanDrift = driftUnlockedAtStart;
            SetDimension(Dimension.Broken);
        }

        private void Update()
        {
            if (!GameplayInputEnabled || Time.timeScale <= 0f)
                return;
            // Timer continues even after Escape releases the cursor. Time.timeScale pauses it.
            if (IsDrifting)
            {
                RemainingDriftTime = Mathf.Max(0f, RemainingDriftTime - Time.deltaTime);
                if (RemainingDriftTime <= 0f)
                {
                    NarrationManager.Announce(Beat.DriftExpired);
                    ReturnToBroken();
                    return; // An expiry-frame Q press cannot immediately restart the timer.
                }
            }

            if (Application.isFocused && Cursor.lockState == CursorLockMode.Locked && Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
                SwitchReality();
        }

        public void SwitchReality()
        {
            if (!initialized || !isActiveAndEnabled || !GameplayInputEnabled)
                return;
            if (IsDrifting)
                ReturnToBroken();
            else
            {
                BeginDrift();
                if (IsDrifting)
                    NarrationManager.Announce(Beat.DriftTimerExplained);
            }
        }

        public void BeginDrift()
        {
            if (!initialized || !isActiveAndEnabled || IsDrifting || !CanDrift || !GameplayInputEnabled)
                return;
            activeDuration = Mathf.Max(0.01f, driftDuration);
            RemainingDriftTime = activeDuration;
            SetDimension(Dimension.Normal);
        }

        public void ReturnToBroken()
        {
            if (!initialized || !IsDrifting)
                return;
            BeforeReturnToBroken?.Invoke();
            RemainingDriftTime = 0f;
            SetDimension(Dimension.Broken);
        }

        public void SetDriftUnlocked(bool unlocked)
        {
            CanDrift = unlocked;
            if (!unlocked)
                ReturnToBroken();
        }

        public void ArriveInBrokenWorld()
        {
            if (!initialized)
                return;
            RemainingDriftTime = 0f;
            SetDimension(Dimension.Broken);
        }

        public void ResetForRespawn()
        {
            if (!initialized)
                return;
            if (IsDrifting)
                BeforeReturnToBroken?.Invoke();
            RemainingDriftTime = driftDuration;
            SetDimension(Dimension.Broken);
        }

        private void SetDimension(Dimension dimension)
        {
            CurrentDimension = dimension;
            GameObject inactiveWorld = dimension == Dimension.Broken ? stableWorld : brokenWorld;
            GameObject activeWorld = dimension == Dimension.Broken ? brokenWorld : stableWorld;
            inactiveWorld.SetActive(false);
            activeWorld.SetActive(true);
            Physics.SyncTransforms();
            DimensionChanged?.Invoke(dimension);
        }

        private void OnDisable()
        {
            // Additive world scenes may already be destroyed when the shared manager shuts down.
            if (initialized && brokenWorld != null && stableWorld != null)
                ReturnToBroken();
        }

        private void OnValidate()
        {
            driftDuration = Mathf.Max(0.01f, driftDuration);
        }
    }
}
