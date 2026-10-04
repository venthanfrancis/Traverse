using System.Collections.Generic;
using UnityEngine;

namespace Drift
{
    public sealed class StorySubtitles : MonoBehaviour
    {
        [SerializeField] private GameFlowManager gameFlow;
        [SerializeField] private RealityManager reality;
        [SerializeField] private NarrationSequence story;

        private readonly Queue<string> pending = new Queue<string>();
        private GameFlowManager.GamePhase previousPhase = GameFlowManager.GamePhase.Title;
        private AnchorController anchors;
        private int heardAnchors;
        private bool heardEcho;
        private bool foundArtifacts;
        private PlayerMovement player;
        private ArtifactInteractable[] caveArtifacts;
        private EchoBlockPuzzle echoPuzzle;
        private bool heardPuzzle;
        private string currentLine;
        private float age, duration;
        private GUIStyle textStyle;

        private void Update()
        {
            if (!DriftSceneLoader.IsReady || gameFlow.IsPaused) return;
            var phase = gameFlow.Phase;
            if (phase != previousPhase)
            {
                previousPhase = phase;
                switch (phase)
                {
                    case GameFlowManager.GamePhase.Title:
                        Clear();
                        heardAnchors = 0;
                        heardEcho = false;
                        foundArtifacts = false;
                        heardPuzzle = false;
                        break;
                    case GameFlowManager.GamePhase.Opening:
                        player = FindFirstObjectByType<PlayerMovement>();
                        caveArtifacts = null;
                        Play(NarrationSequence.NarrationBeats.OpeningNarration);
                        break;
                    case GameFlowManager.GamePhase.BrokenWorld:
                        Clear();
                        Play(NarrationSequence.NarrationBeats.ArtifactArrival);
                        break;
                    case GameFlowManager.GamePhase.FinalEscape:
                        Clear();
                        Play(NarrationSequence.NarrationBeats.FinalAnchor);
                        break;
                    case GameFlowManager.GamePhase.Returning:
                        Clear();
                        break;
                    case GameFlowManager.GamePhase.EndingWalk:
                        Play(NarrationSequence.NarrationBeats.ReturnToCave);
                        break;
                }
            }

            if (phase == GameFlowManager.GamePhase.Opening && !foundArtifacts && gameFlow.IsGameplayRunning && player != null)
            {
                if (caveArtifacts == null) caveArtifacts = FindObjectsByType<ArtifactInteractable>(FindObjectsSortMode.None);
                foreach (var artifact in caveArtifacts)
                    if (artifact != null && Vector3.Distance(player.transform.position, artifact.transform.position) < 8f)
                    {
                        foundArtifacts = true;
                        Play(NarrationSequence.NarrationBeats.FoundArtifacts);
                        break;
                    }
            }

            if (phase == GameFlowManager.GamePhase.BrokenWorld)
            {
                if (!heardPuzzle)
                {
                    if (echoPuzzle == null) echoPuzzle = FindFirstObjectByType<EchoBlockPuzzle>();
                    if (echoPuzzle != null && player != null && Vector3.Distance(player.transform.position, echoPuzzle.transform.position) < 7f)
                    {
                        heardPuzzle = true;
                        Play(NarrationSequence.NarrationBeats.EchoPuzzle);
                    }
                }
                if (anchors == null) anchors = FindFirstObjectByType<AnchorController>();
                if (anchors != null && anchors.ActiveNodes > heardAnchors)
                {
                    while (heardAnchors < anchors.ActiveNodes)
                    {
                        heardAnchors++;
                        if (heardAnchors == 1) Play(NarrationSequence.NarrationBeats.FirstAnchor);
                        if (heardAnchors == 2) Play(NarrationSequence.NarrationBeats.SecondAnchor);
                    }
                }
                if (!heardEcho && reality.IsDrifting)
                {
                    heardEcho = true;
                    Play(NarrationSequence.NarrationBeats.FirstEcho);
                }
            }

            if (!gameFlow.IsGameplayRunning) return;
            if (PlayerPrefs.GetInt("Drift.Subtitles", 1) == 0)
            {
                Clear();
                return;
            }
            if (currentLine == null && pending.Count > 0)
            {
                currentLine = pending.Dequeue();
                age = 0f;
                duration = Mathf.Clamp(currentLine.Length * .055f, 4f, 7f);
            }
            if (currentLine != null)
            {
                age += Time.deltaTime;
                if (age >= duration) currentLine = null;
            }
        }

        private void Play(NarrationSequence.NarrationBeats beat)
        {
            if (story == null || PlayerPrefs.GetInt("Drift.Subtitles", 1) == 0) return;
            var lines = story.Lines(beat);
            if (lines == null) return;
            foreach (string line in lines)
                if (!string.IsNullOrWhiteSpace(line)) pending.Enqueue(line);
        }

        private void Clear()
        {
            pending.Clear();
            currentLine = null;
        }

        private void OnGUI()
        {
            if (currentLine == null || gameFlow.IsPaused || !gameFlow.IsGameplayRunning || PlayerPrefs.GetInt("Drift.Subtitles", 1) == 0) return;
            if (textStyle == null)
                textStyle = GameUI.Text(15);
            Matrix4x4 matrix = GameUI.Begin();
            Color color = GUI.color;
            float alpha = Mathf.Min(Mathf.Clamp01(age / .2f), Mathf.Clamp01((duration - age) / .35f));
            var area = new Rect(230f, 398f, 500f, 42f);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GameUI.Label(new Rect(area.x + 12f, area.y + 4f, area.width - 24f, area.height - 8f), currentLine, textStyle);
            GUI.color = color;
            GUI.matrix = matrix;
        }
    }
}
