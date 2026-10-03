using System.Collections;
using UnityEngine;

namespace Drift
{
    public sealed class GameAudioManager : MonoBehaviour
    {
        [SerializeField]
        private GameFlowManager gameFlow;
        [SerializeField]
        private RealityManager realityManager;
        [SerializeField]
        private OpeningSequence openingSequence;
        [Header("Ambience")]
        [SerializeField]
        private AudioClip caveAmbience, brokenAmbience, normalAmbience;
        [Header("One-shot cues")]
        [SerializeField]
        private AudioClip driftEnter, driftExit, driftWarning, artifact, anchor, uiClick;
        private AudioSource ambienceSource, ambienceOther, cueSource, uiSource;
        private AudioClip selectedAmbience;
        private Coroutine ambienceFade;
        private bool artifactPlaying;
        private void Awake()
        {
            ambienceSource = Source(true, 0.25f);
            ambienceOther = Source(true, 0f);
            cueSource = Source(false, 0.5f);
            uiSource = Source(false, 0.4f);
            uiSource.ignoreListenerPause = true;
        }

        private AudioSource Source(bool loop, float volume)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.volume = volume;
            source.spatialBlend = 0f;
            return source;
        }

        private void Update()
        {
            bool cave = gameFlow.Phase == GameFlowManager.GamePhase.Title || gameFlow.Phase == GameFlowManager.GamePhase.Opening || gameFlow.Phase == GameFlowManager.GamePhase.EndingWalk || gameFlow.Phase == GameFlowManager.GamePhase.Ending || gameFlow.Phase == GameFlowManager.GamePhase.Returning;
            AudioClip ambience = cave ? caveAmbience : realityManager.IsDrifting ? normalAmbience : brokenAmbience;
            if (ambience != selectedAmbience)
            {
                selectedAmbience = ambience;
                if (ambienceFade != null)
                    StopCoroutine(ambienceFade);
                ambienceFade = StartCoroutine(CrossfadeAmbience(ambience));
            }

            if (openingSequence.IsTransitioning && !artifactPlaying)
            {
                artifactPlaying = true;
                PlayArtifact();
            }
        }

        private IEnumerator CrossfadeAmbience(AudioClip clip)
        {
            AudioSource outgoing = ambienceSource, incoming = ambienceOther;
            float oldVolume = outgoing.volume;
            incoming.Stop();
            incoming.clip = clip;
            incoming.volume = 0f;
            if (clip != null)
                incoming.Play();
            ambienceSource = incoming;
            ambienceOther = outgoing;
            for (float t = 0; t < .45f; t += Time.unscaledDeltaTime)
            {
                float blend = t / .45f;
                incoming.volume = .25f * blend;
                outgoing.volume = oldVolume * (1f - blend);
                yield return null;
            }

            incoming.volume = .25f;
            outgoing.volume = 0f;
            outgoing.Stop();
        }

        private void Cue(AudioClip clip)
        {
            if (clip != null)
                cueSource.PlayOneShot(clip);
        }

        public void PlayDrift(bool entering, bool forced = false)
        {
            AudioClip clip = entering ? driftEnter : driftExit;
            if (clip != null)
                cueSource.PlayOneShot(clip, forced ? 1.25f : 1f);
        }

        public void PlayWarning() => Cue(driftWarning);
        public void PlayArtifact() => Cue(artifact);
        public void PlayAnchor() => Cue(anchor);
        public void PlayUI()
        {
            if (uiClick != null)
                uiSource.PlayOneShot(uiClick);
        }
    }
}
