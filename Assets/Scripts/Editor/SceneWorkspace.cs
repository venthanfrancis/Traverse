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
            EditorApplication.delayCall += Configure;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                    PrepareForPlay();
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
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.name == "SharedSystems" || root.name == "CaveContent" || root.name == "BrokenWorldContent")
                        root.SetActive(false);
                    if (root.name == "BrokenWorldContent")
                        foreach (string name in new[]
                        {
                            "BrokenWorld",
                            "NormalWorld"
                        }

                        )
                        {
                            Transform world = root.transform.Find("Environment/" + name);
                            if (world != null)
                                world.gameObject.SetActive(false);
                        }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Configure();
        }

        [MenuItem("DRIFT/Scenes/Edit Cave")]
        public static void Cave() => Edit("Cave");
        [MenuItem("DRIFT/Scenes/Edit Broken World")]
        public static void Broken() => Edit("BrokenWorld");
        [MenuItem("DRIFT/Scenes/Open Main Menu")]
        public static void Menu() => Edit("MainMenu");
        private static void Edit(string name)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            PrepareForPlay();
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            if (name != "MainMenu")
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    root.SetActive(true);
                    if (name == "BrokenWorld")
                    {
                        Transform world = root.transform.Find("Environment/BrokenWorld");
                        if (world != null)
                            world.gameObject.SetActive(true);
                    }

                    Selection.activeGameObject = root;
                }

                SceneView.lastActiveSceneView?.FrameSelected();
            }

            Configure();
        }
    }
}
