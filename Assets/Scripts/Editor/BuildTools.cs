using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Drift.Editor
{
    public static class BuildTools
    {
        [MenuItem("DRIFT/Build Windows")]
        public static void BuildWindows()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            SceneSetup[] workspace = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (EditorBuildSettingsScene level in EditorBuildSettings.scenes)
                    if (level.enabled && !SceneManager.GetSceneByPath(level.path).isLoaded)
                        EditorSceneManager.OpenScene(level.path, OpenSceneMode.Additive);
                SceneWorkspace.PrepareForPlay();
                EditorSceneManager.SaveOpenScenes();
                Directory.CreateDirectory("Builds/Windows");
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = System.Array.ConvertAll(System.Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled), scene => scene.path), locationPathName = "Builds/Windows/DRIFT.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
                Debug.Log($"Windows build: {report.summary.result}, {report.summary.totalErrors} errors.");
                if (report.summary.result != BuildResult.Succeeded)
                    throw new System.InvalidOperationException("Windows build failed.");
            }
            finally
            {
                if (System.Array.Exists(workspace, scene => scene.isLoaded && scene.isActive))
                    EditorSceneManager.RestoreSceneManagerSetup(workspace);
                else
                    EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            }
        }

        [MenuItem("DRIFT/Check Gameplay")]
        private static void CheckGameplay()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Start a fresh Play session at the title first.");
                return;
            }

            GameJamVerification.StartCheck(false);
        }
    }
}
