using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<T>(FindObjectsInactive.Include);
                if (instance == null)
                {
                    GameObject singleton = new GameObject(typeof(T) + " (Singleton)");
                    instance = singleton.AddComponent<T>();
                }
            }
            return instance;
        }
    }

    // Unlike Instance, never creates one.
    public static T Existing => instance;

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
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
