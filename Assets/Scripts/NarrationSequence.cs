using UnityEngine;

[CreateAssetMenu(menuName = "Narration Sequence")]
public class NarrationSequence : ScriptableObject
{
    // Append only: the numbers are serialized in NarrationSequence.asset and in scene triggers.
    public enum NarrationBeats
    {
        None = 0,
        OpeningNarration = 1,
        DeathNarration = 2,
        WinningNarration = 3,
        TutorialDrift = 4,
        TutorialInteract = 5,
        BrokenWorldIntro = 6,
        NormalWorldIntro = 7,
        DriftTimerExplained = 8,
        DriftExpired = 9,
        Respawned = 10,
        CheckpointReached = 11,
        NodeWrongOrder = 12,
        NodeRestoredFirst = 13,
        NodeRestoredSecond = 14,
        TelescopeAligned = 15,
        EscapeStart = 16,
        CollapseWarning = 17,
        ReturnHome = 18,
        // Placed by hand with NarrationTrigger volumes.
        SwitchHint = 19,
        NodeIntro = 20,
        NodeLocation = 21,
        TelescopeHint = 22,
    }

    [System.Serializable]
    public struct Beat
    {
        public NarrationBeats beat;
        [TextArea(2, 5)] public string[] lines;
    }

    public Beat[] beats;

    public string[] Lines(NarrationBeats wanted)
    {
        for (int i = 0; i < beats.Length; i++)
            if (beats[i].beat == wanted) return beats[i].lines;

        return null;
    }
}
