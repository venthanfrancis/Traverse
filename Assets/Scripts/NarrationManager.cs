using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static NarrationSequence;

public class NarrationManager : Singleton<NarrationManager>
{
    public bool isNarrating = false;
    public GameObject narrativeBox;
    public TextMeshProUGUI conversationBox;

    [SerializeField] private NarrationSequence sequences;
    [SerializeField] private float charactersPerSecond = 30f;
    [Tooltip("Key that advances a line")]
    [SerializeField] private KeyCode advanceKey = KeyCode.E;

    private struct Narration
    {
        public string[] lines;
        public float delay;
    }

    private readonly Queue<Narration> pending = new Queue<Narration>();
    private Coroutine playing;

    public void Play(NarrationBeats narrationBeat, float delay)
    {
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
            yield return WaitForKey();

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

    private IEnumerator WaitForKey()
    {
        yield return null;

        while (!SkipPressed) yield return null;

        yield return null;
    }

    private bool SkipPressed => Input.GetKeyDown(advanceKey);
}
