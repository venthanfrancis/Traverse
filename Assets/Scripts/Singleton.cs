using UnityEngine;
using UnityEngine.SceneManagement;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                if (instance == null)
                {
                    GameObject singleton = new GameObject(typeof(T) + " (Singleton)");
                    instance = singleton.AddComponent<T>();
                }
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
        }
        else if (instance != this)
        {
            Destroy(this);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected virtual void OnDestroy()
    {    
        if (instance == this)
        {
            instance = null;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Replace "GameplayScene" with the name of your gameplay scene
        if (scene.name != "Gameplay")
        {
            Destroy(gameObject);
        }
    }
}