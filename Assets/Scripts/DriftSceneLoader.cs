using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Drift
{
    // Loads level content once. Reality switching still only toggles GameObjects.
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
            yield return SceneManager.LoadSceneAsync("Cave", LoadSceneMode.Additive);
            yield return SceneManager.LoadSceneAsync("BrokenWorld", LoadSceneMode.Additive);
            var identities = new Dictionary<string, GameObject>();
            foreach (SceneLinkId identity in FindObjectsByType<SceneLinkId>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                identities.Add(identity.Id, identity.gameObject);
            foreach (Link link in links)
            {
                Component owner = identities[link.owner].GetComponents<Component>()[link.ownerComponent];
                GameObject target = identities[link.target];
                UnityEngine.Object value = link.targetComponent < 0 ? target : target.GetComponents<Component>()[link.targetComponent];
                Assign(owner, link.field, value);
            }

            // Restore references before any gameplay Awake/OnEnable/Start runs.
            foreach (string name in new[]
            {
                "Cave",
                "BrokenWorld"
            }

            )
                foreach (GameObject root in SceneManager.GetSceneByName(name).GetRootGameObjects())
                    root.SetActive(true);
            sharedSystems.SetActive(true);
            IsReady = true;
            Debug.Log("DRIFT_SCENES_READY: MainMenu + Cave + BrokenWorld; cross-scene references restored.");
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
                GUI.Label(new Rect(20, 20, 300, 40), "Loading DRIFT…");
        }

        private void OnDestroy()
        {
            IsReady = false;
        }
    }
}
