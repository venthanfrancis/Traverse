using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Drift
{
    // Only the current level stays loaded. Shared player systems survive scene changes.
    public sealed class DriftSceneLoader : MonoBehaviour
    {
        [Serializable]
        public sealed class Link
        {
            public string owner, target, field;
            public int ownerComponent, targetComponent;
        }

        [SerializeField]
        private GameObject sharedSystems;
        [SerializeField]
        private Link[] links;
        public static bool IsReady { get; private set; }

        private IEnumerator Start()
        {
            IsReady = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            foreach (string level in new[] { "Cave", "BrokenWorld" })
                if (SceneManager.GetSceneByName(level).isLoaded)
                    yield return SceneManager.UnloadSceneAsync(level);
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(sharedSystems);
            sharedSystems.SetActive(true);
            IsReady = true;
        }

        public IEnumerator LoadLevel(string name)
        {
            IsReady = false;
            yield return SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
            var identities = new Dictionary<string, GameObject>();
            foreach (SceneLinkId identity in FindObjectsByType<SceneLinkId>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                identities.Add(identity.Id, identity.gameObject);
            foreach (Link link in links)
            {
                if (!identities.TryGetValue(link.owner, out GameObject ownerObject))
                    continue;
                Component owner = ownerObject.GetComponents<Component>()[link.ownerComponent];
                UnityEngine.Object value = null;
                if (identities.TryGetValue(link.target, out GameObject target))
                    value = link.targetComponent < 0 ? target : target.GetComponents<Component>()[link.targetComponent];
                Assign(owner, link.field, value);
            }
            Transform content = SceneManager.GetSceneByName(name).GetRootGameObjects()[0].transform;
            Transform broken = content.Find("Environment/BrokenWorld");
            Transform normal = content.Find("Environment/NormalWorld");
            sharedSystems.GetComponentInChildren<RealityManager>(true).BindWorlds(broken != null ? broken.gameObject : null, normal != null ? normal.gameObject : null);
            foreach (GameObject root in SceneManager.GetSceneByName(name).GetRootGameObjects())
                root.SetActive(true);
            content.gameObject.AddComponent<GameplayVfx>();
            IsReady = true;
        }

        public void ReturnToMenu()
        {
            Destroy(sharedSystems);
            Destroy(gameObject);
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }

        private static void Assign(Component owner, string path, UnityEngine.Object value)
        {
            string[] parts = path.Split('.');
            FieldInfo field = owner.GetType().GetField(parts[0], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                throw new InvalidOperationException("Missing scene reference field: " + path);
            if (parts.Length == 1)
                field.SetValue(owner, value);
            else if (parts.Length == 3 && parts[1] == "Array")
            {
                int index = int.Parse(parts[2].Substring(5, parts[2].Length - 6));
                ((IList)field.GetValue(owner))[index] = value;
            }
            else
                throw new InvalidOperationException("Unsupported scene reference path: " + path);
        }

        private void OnGUI()
        {
            if (!IsReady)
            {
                if (loadingStyle == null) loadingStyle = GameUI.Text(12);
                Matrix4x4 previous = GameUI.Begin();
                GameUI.Label(new Rect(360f, 250f, 240f, 40f), "Loading…", loadingStyle);
                GUI.matrix = previous;
            }
        }
        private GUIStyle loadingStyle;

        private void OnDestroy()
        {
            IsReady = false;
        }
    }
}
