using UnityEngine;

namespace Drift
{
    public sealed class ArtifactPresentation : MonoBehaviour
    {
        [SerializeField]
        private bool corrupted;
        [SerializeField]
        private OpeningSequence opening;
        [SerializeField]
        private GameFlowManager flow;
        [SerializeField]
        private Transform player;
        [SerializeField]
        private Transform[] pieces;
        [SerializeField]
        private Light glow;
        [SerializeField]
        private ParticleSystem motes;
        [SerializeField]
        private AudioSource hum;
        [SerializeField]
        private CameraShake shake;
        private Vector3[] origins;
        private Quaternion[] rotations;
        private float age, reaction;
        private bool reacting;
        private bool darkened, litAgain;
        private void Awake()
        {
            origins = new Vector3[pieces.Length];
            rotations = new Quaternion[pieces.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                origins[i] = pieces[i].localPosition;
                rotations[i] = pieces[i].localRotation;
            }
        }

        private void Update()
        {
            if (flow.IsPaused || flow.Phase == GameFlowManager.GamePhase.Title)
                return;
            bool dead = corrupted && opening.HasArrived;
            bool twist = dead && (flow.Phase == GameFlowManager.GamePhase.Returning || flow.Phase == GameFlowManager.GamePhase.Ending) && glow != null && glow.enabled;
            if (dead)
            {
                if (!darkened || (twist && !litAgain))
                {
                    var block = new MaterialPropertyBlock();
                    block.SetColor("_BaseColor", new Color(.06f, .04f, .05f));
                    block.SetColor("_EmissionColor", twist ? new Color(.25f, .03f, .1f) : Color.black);
                    foreach (Renderer r in GetComponentsInChildren<Renderer>())
                        if (!(r is ParticleSystemRenderer))
                            r.SetPropertyBlock(block);
                    darkened = true;
                    litAgain = twist;
                }

                if (hum != null)
                    hum.volume = 0f;
                if (motes != null && motes.isPlaying)
                    motes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                return;
            }

            age += Time.deltaTime;
            bool activeReaction = opening.IsTransitioning && opening.Initiator == gameObject;
            if (activeReaction && !reacting)
            {
                reacting = true;
                shake.Shake(1.1f, .045f);
            }

            if (activeReaction)
                reaction = Mathf.Min(1f, reaction + Time.deltaTime / 1.1f);
            for (int i = 0; i < pieces.Length; i++)
            {
                float phase = age * (corrupted ? 2f : .5f) + i * 1.9f;
                Vector3 offset = Vector3.up * Mathf.Sin(phase) * (corrupted ? .12f : .035f);
                if (corrupted)
                    offset.x = Mathf.Sin(phase * 2.1f) * .055f;
                pieces[i].localPosition = origins[i] + offset * (1f + reaction * 2f);
                pieces[i].localRotation = rotations[i] * Quaternion.Euler(0, age * (corrupted ? 18f : 7f), corrupted ? Mathf.Sin(phase) * 7f : 0f);
            }

            if (glow != null && !activeReaction)
                glow.intensity = corrupted ? 2f + Mathf.Pow(Mathf.Sin(age * 4f), 2) * 1.4f : 1.6f;
            if (motes != null)
            {
                var emission = motes.emission;
                emission.rateOverTime = (corrupted ? 10f : 5f) + reaction * 35f;
            }

            if (hum != null)
            {
                float distance = Vector3.Distance(player.position, transform.position);
                hum.volume = (corrupted ? .22f : .12f) * Mathf.Clamp01(1f - distance / 15f) + reaction * .15f;
                hum.pitch = corrupted ? 1f + Mathf.Sin(age * 3f) * .025f + reaction * .2f : 1f;
            }
        }
    }
}
