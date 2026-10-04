using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Drift.Editor
{
    [InitializeOnLoad]
    public static class SceneWorkspace
    {
        static SceneWorkspace()
        {
            EditorApplication.delayCall += () =>
            {
                Configure();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    Preview(SceneManager.GetSceneAt(i), false);
            };
            EditorSceneManager.sceneOpened += (scene, mode) =>
            {
                RememberScene(scene);
                Preview(scene, false);
            };
            EditorSceneManager.sceneSaving += (scene, path) =>
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Transform normal = root.transform.Find("Environment/NormalWorld");
                    if (normal != null)
                        SessionState.SetBool("Drift.NormalPreview." + scene.path, normal.gameObject.activeSelf);
                }
                PrepareRoots(scene);
            };
            EditorSceneManager.sceneSaved += scene =>
            {
                RememberScene(scene);
                EditorApplication.delayCall += () =>
                {
                    Preview(scene, SessionState.GetBool("Drift.NormalPreview." + scene.path, false));
                };
            };
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                    PrepareForPlay();
                if (state == PlayModeStateChange.EnteredEditMode)
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                        Preview(SceneManager.GetSceneAt(i), false);
            };
        }

        public static void Configure()
        {
            if (!File.Exists("Assets/Scenes/MainMenu.unity"))
                return;
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MainMenu.unity");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Cave.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/BrokenWorld.unity", true)
            };
        }

        public static void PrepareForPlay()
        {
            if (!File.Exists("Assets/Scenes/MainMenu.unity"))
                return;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name != "MainMenu" && scene.name != "Cave" && scene.name != "BrokenWorld")
                    continue;
                string key = "Drift.SceneTimestamp." + scene.path;
                string loaded = SessionState.GetString(key, "");
                bool changedOnDisk = File.Exists(scene.path) && loaded != "" && loaded != File.GetLastWriteTimeUtc(scene.path).Ticks.ToString();
                PrepareRoots(scene);

                if (!changedOnDisk)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                else
                    Debug.LogWarning("Scene changed on disk: reopen " + scene.name + " before editing it. The older loaded copy was not saved.");
            }

            Configure();
        }

        [MenuItem("DRIFT/Scenes/Edit Cave")]
        public static void Cave() => Edit("Cave");
        [MenuItem("DRIFT/Scenes/Edit Broken World")]
        public static void Broken() => Edit("BrokenWorld");
        [MenuItem("DRIFT/Scenes/Edit Normal World")]
        public static void Normal() => Edit("BrokenWorld", true);
        [MenuItem("DRIFT/Scenes/Open Main Menu")]
        public static void Menu() => Edit("MainMenu");
        private static void Preview(Scene scene, bool normal)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer || !scene.isLoaded || (scene.name != "Cave" && scene.name != "BrokenWorld"))
                return;
            if (SessionState.GetString("Drift.SceneTimestamp." + scene.path, "") == "")
                RememberScene(scene);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                root.SetActive(true);
                if (scene.name == "BrokenWorld")
                {
                    root.transform.Find("Environment/NormalWorld")?.gameObject.SetActive(normal);
                    root.transform.Find("Environment/BrokenWorld")?.gameObject.SetActive(!normal);
                }
            }
        }

        private static void RememberScene(Scene scene)
        {
            if (File.Exists(scene.path))
                SessionState.SetString("Drift.SceneTimestamp." + scene.path, File.GetLastWriteTimeUtc(scene.path).Ticks.ToString());
        }

        private static void PrepareRoots(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "SharedSystems" || root.name == "CaveContent" || root.name == "BrokenWorldContent")
                    root.SetActive(false);
                if (root.name == "BrokenWorldContent")
                    foreach (string world in new[] { "NormalWorld", "BrokenWorld" })
                        root.transform.Find("Environment/" + world)?.gameObject.SetActive(false);
            }
        }

        private static void Edit(string name, bool normal = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            PrepareForPlay();
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            Preview(scene, normal);
            if (name != "MainMenu")
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    root.SetActive(true);
                    Selection.activeGameObject = root;
                }

                SceneView.lastActiveSceneView?.FrameSelected();
            }

            Configure();
        }
    }
}
