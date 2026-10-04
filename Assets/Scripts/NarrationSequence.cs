using UnityEngine;

[CreateAssetMenu(menuName = "Narration Sequence")]
public class NarrationSequence : ScriptableObject
{
    public enum NarrationBeats
    {
        None = 0,
        OpeningNarration = 1,
        DeathNarration = 2,
        WinningNarration = 3,
        ArtifactArrival = 4,
        FirstEcho = 5,
        FirstAnchor = 6,
        SecondAnchor = 7,
        FinalAnchor = 8,
        ReturnToCave = 9,
        FoundArtifacts = 10,
        EchoPuzzle = 11,
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
