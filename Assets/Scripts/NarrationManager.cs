using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static NarrationSequence;

public class NarrationManager : Singleton<NarrationManager>
{
    public bool isNarrating = false;
    public GameObject narrativeBox;
    public TextMeshProUGUI conversationBox;

    [SerializeField] private NarrationSequence sequences;
    [SerializeField] private float charactersPerSecond = 30f;
    [Tooltip("Keys that skip typing or advance a line early. While a line is showing, E is used up by the subtitle and does not interact.")]
    [SerializeField] private Key[] advanceKeys = { Key.Enter, Key.E };
    [Tooltip("When on, a finished line advances by itself after its read time.")]
    [SerializeField] private bool autoAdvance = true;
    [SerializeField, Min(0f)] private float minReadSeconds = 2f;
    [SerializeField, Min(0f)] private float secondsPerCharacter = 0.05f;

    private struct Narration
    {
        public string[] lines;
        public float delay;
    }

    private readonly Queue<Narration> pending = new Queue<Narration>();
    private Coroutine playing;

    private readonly HashSet<NarrationBeats> played = new HashSet<NarrationBeats>();

    // True while a subtitle line is on screen. E belongs to the subtitle then (it skips/advances
    // the line), so PlayerInteraction ignores E until the line is gone.
    public static bool IsShowingLine
    {
        get
        {
            NarrationManager manager = Existing;
            return manager != null && manager.conversationBox != null && manager.conversationBox.gameObject.activeInHierarchy;
        }
    }

    // Used by game code. Does nothing when there is no manager (tests, empty scenes)
    // and, by default, plays each beat only once per session.
    public static void Announce(NarrationBeats narrationBeat, float delay = 0f, bool once = true)
    {
        NarrationManager manager = Existing;
        if (manager == null) return;

        if (once) manager.PlayOnce(narrationBeat, delay);
        else manager.Play(narrationBeat, delay);
    }

    public void PlayOnce(NarrationBeats narrationBeat, float delay = 0f)
    {
        if (played.Add(narrationBeat)) Play(narrationBeat, delay);
    }

    protected override void Awake()
    {
        base.Awake();

        // The boxes stay enabled in the scene for editing; they only show while narrating.
        if (narrativeBox != null) narrativeBox.SetActive(false);
        if (conversationBox != null) conversationBox.gameObject.SetActive(false);
    }

    public void Play(NarrationBeats narrationBeat, float delay = 0f)
    {
        if (sequences == null || narrativeBox == null || conversationBox == null)
        {
            Debug.LogError("NarrationManager is missing its sequence or UI references.", this);
            return;
        }

        string[] lines = sequences.Lines(narrationBeat);

        if (lines == null)
        {
            Debug.LogWarning($"No lines for {narrationBeat}", this);
            return;
        }

        Enqueue(lines, delay);
    }

    private void Enqueue(string[] lines, float delay)
    {
        pending.Enqueue(new Narration { lines = lines, delay = delay });
        if (playing == null) PlayNext();
    }

    private void PlayNext()
    {
        if (pending.Count > 0)
        {
            playing = StartCoroutine(PlayRoutine(pending.Dequeue()));
            return;
        }

        playing = null;

        if (!isNarrating) return;
        isNarrating = false;
        narrativeBox.SetActive(false);
    }

    private IEnumerator PlayRoutine(Narration narration)
    {
        yield return new WaitForSeconds(narration.delay);

        if (!isNarrating)
        {
            isNarrating = true;
            narrativeBox.SetActive(true);
        }

        for (int i = 0; i < narration.lines.Length; i++)
        {
            TextMeshProUGUI box = conversationBox;
            box.gameObject.SetActive(true);

            yield return TypeRoutine(box, narration.lines[i]);
            yield return WaitForKey(narration.lines[i]);

            box.gameObject.SetActive(false);
        }

        PlayNext();
    }


    private IEnumerator TypeRoutine(TextMeshProUGUI box, string line)
    {
        box.text = "";

        // Per frame, or GetKeyDown lands between characters.
        float shown = 0f;

        while (shown < line.Length)
        {
            if (SkipPressed) break;

            shown += charactersPerSecond * Time.deltaTime;
            box.text = line.Substring(0, Mathf.Min((int)shown, line.Length));
            yield return null;
        }

        box.text = line;
    }

    private IEnumerator WaitForKey(string line)
    {
        yield return null;

        // Unscaled so a line never hangs just because Time.timeScale is 0.
        float readTime = minReadSeconds + line.Length * secondsPerCharacter;
        float elapsed = 0f;

        while (!SkipPressed && (!autoAdvance || elapsed < readTime))
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return null;
    }

    private bool SkipPressed
    {
        get
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;

            foreach (Key key in advanceKeys)
                if (keyboard[key].wasPressedThisFrame) return true;

            return false;
        }
    }
}
