using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public bool gamePaused = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        NarrationManager.Instance.Play(NarrationSequence.NarrationBeats.OpeningNarration, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
