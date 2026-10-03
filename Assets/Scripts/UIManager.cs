using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

    [SerializeField] GameObject PauseMenu;
    [SerializeField] Image winTransitionImage;
    [SerializeField] Image deathTransitionImage;

    void Start()
    {
        PauseMenu.SetActive(false);
        StartCoroutine(FadeOut());
    }

    
    void Update()
    {
        

        //if (Input.GetKeyUp(KeyCode.P))
            //TogglePauseMenu();
    }

    //public void TogglePauseMenu()
    //{
    //    GameManager.Instance.gamePaused = !GameManager.Instance.gamePaused;
    //    if (GameManager.Instance.gamePaused )
    //    {
    //        Cursor.lockState = CursorLockMode.None;
    //        Time.timeScale = 0;
    //    }
    //    else
    //    {
    //        Cursor.lockState = CursorLockMode.Locked;
    //        Time.timeScale = 1;
    //    }
    //    PauseMenu.SetActive(GameManager.Instance.gamePaused);
    //}

    public void RestartGame()
    {
        SceneManager.LoadScene("Gameplay");
        gameObject.SetActive(false);
    }

    IEnumerator FadeOut()
    {
        winTransitionImage.gameObject.SetActive(true);
        float currentFadeTime = 0f;
        float fadeOutInSeconds = 1f;
        Color c = winTransitionImage.color;
        while (currentFadeTime < fadeOutInSeconds)
        {
            currentFadeTime += Time.deltaTime;
            c.a = 1 - Mathf.Clamp(currentFadeTime / fadeOutInSeconds, 0f, 1f);
            winTransitionImage.color = c;
            yield return null;
        }
    }
}
