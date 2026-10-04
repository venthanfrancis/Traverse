using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Drift
{
    // Runs before gameplay input so Escape cannot conflict with the orbit camera.
    [DefaultExecutionOrder(-300)]
    public sealed class GameFlowManager : MonoBehaviour
    {
        public enum GamePhase
        {
            Title,
            Opening,
            BrokenWorld,
            FinalEscape,
            Returning,
            EndingWalk,
            Ending
        }

        [SerializeField]
        private RealityManager realityManager;
        [SerializeField]
        private OpeningSequence openingSequence;
        [SerializeField]
        private PlayerMovement player;
        [SerializeField]
        private PlayerInteraction interaction;
        [SerializeField]
        private PlayerRespawn respawn;
        [SerializeField]
        private ThirdPersonCamera playerCamera;
        [SerializeField]
        private ScreenFade fade;
        [SerializeField]
        private GameObject openingCave, brokenWorld, normalWorld;
        [SerializeField]
        private Transform entranceSpawn, caveReturnSpawn;
        [SerializeField]
        private ArtifactInteractable corruptedArtifact;
        [SerializeField]
        private Light corruptedLight;
        [SerializeField]
        private GameObject endingFragment;
        [SerializeField]
        private GameAudioManager audioManager;
        public GamePhase Phase { get; private set; } = GamePhase.Title;
        public bool IsPaused { get; private set; }
        public bool IsGameplayRunning => DriftSceneLoader.IsReady && !loadingOpening && !IsPaused && Phase != GamePhase.Title && Phase != GamePhase.Ending && Phase != GamePhase.Returning && !openingSequence.IsTransitioning;

        private bool settingsOpen;
        private bool loadingOpening;
        private float tutorialUntil;
        private float endingShownAt;
        private GUIStyle titleStyle, labelStyle;
        private static bool playAfterReload;
        private void Awake()
        {
            AudioListener.volume = PlayerPrefs.GetFloat("Drift.MasterVolume", 0.7f);
            if (playerCamera != null)
                playerCamera.MouseSensitivity = PlayerPrefs.GetFloat("Drift.MouseSensitivity", 0.12f);
        }

        private void Start()
        {
            if (playAfterReload)
            {
                playAfterReload = false;
                Play();
            }
            else
            {
                Time.timeScale = 0f;
                SetControl(false);
            }
        }

        private void Update()
        {
            if (!DriftSceneLoader.IsReady || loadingOpening)
                return;
            if (Phase == GamePhase.Opening && openingSequence.HasArrived)
                Phase = GamePhase.BrokenWorld;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (Phase == GamePhase.Title)
                    settingsOpen = false;
                else if (Phase != GamePhase.Returning && Phase != GamePhase.Ending && !openingSequence.IsTransitioning)
                {
                    if (IsPaused)
                        Resume();
                    else
                        Pause();
                }
            }
        }

        public void Play()
        {
            if (Phase != GamePhase.Title)
                return;
            Phase = GamePhase.Opening;
            loadingOpening = true;
            settingsOpen = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            tutorialUntil = Time.unscaledTime + 9f;
            SetControl(false);
            StartCoroutine(LoadOpening());
        }

        private IEnumerator LoadOpening()
        {
            yield return fade.FadeOut(.25f);
            yield return FindFirstObjectByType<DriftSceneLoader>().LoadLevel("Cave");
            openingSequence.EnterCave();
            yield return fade.FadeIn(.25f);
            loadingOpening = false;
            SetControl(true);
        }

        public void Pause()
        {
            if (!IsGameplayRunning)
                return;
            IsPaused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            SetControl(false);
        }

        public void Resume()
        {
            if (!IsPaused)
                return;
            IsPaused = false;
            settingsOpen = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SetControl(true);
        }

        public void RestartCheckpoint()
        {
            if (!IsPaused)
                return;
            if (Phase == GamePhase.Opening)
                PlacePlayer(entranceSpawn);
            else if (Phase == GamePhase.EndingWalk)
                PlacePlayer(caveReturnSpawn);
            else
                respawn.Respawn();
            Resume();
        }

        public void MainMenu()
        {
            playAfterReload = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            FindFirstObjectByType<DriftSceneLoader>().ReturnToMenu();
        }

        public void PlayAgain()
        {
            playAfterReload = true;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            FindFirstObjectByType<DriftSceneLoader>().ReturnToMenu();
        }

        public void BeginEscape()
        {
            if (Phase == GamePhase.FinalEscape || Phase == GamePhase.Returning || Phase == GamePhase.EndingWalk || Phase == GamePhase.Ending)
                return;
            Phase = GamePhase.FinalEscape;
            audioManager.PlayAnchor();
        }

        public void EscapeThroughPortal()
        {
            if (Phase != GamePhase.FinalEscape || IsPaused)
                return;
            Phase = GamePhase.Returning;
            StartCoroutine(ReturnToCave());
        }

        private IEnumerator ReturnToCave()
        {
            SetControl(false);
            audioManager.PlayAnchor();
            yield return fade.FadeOut(0.9f);
            realityManager.SetDriftUnlocked(false);
            yield return FindFirstObjectByType<DriftSceneLoader>().LoadLevel("Cave");
            openingCave.SetActive(true);
            corruptedArtifact.enabled = false;
            foreach (ParticleSystem particles in corruptedArtifact.GetComponentsInChildren<ParticleSystem>())
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (corruptedLight != null)
                corruptedLight.enabled = false;
            Renderer renderer = corruptedArtifact.GetComponent<Renderer>();
            if (renderer != null)
            {
                MaterialPropertyBlock color = new MaterialPropertyBlock();
                color.SetColor("_BaseColor", Color.black);
                color.SetColor("_EmissionColor", Color.black);
                renderer.SetPropertyBlock(color);
            }

            PlacePlayer(caveReturnSpawn);
            yield return fade.FadeIn(0.9f);
            Phase = GamePhase.EndingWalk;
            SetControl(true);
        }

        public void FinishEnding()
        {
            if (Phase != GamePhase.EndingWalk || IsPaused)
                return;
            Phase = GamePhase.Returning;
            StartCoroutine(Ending());
        }

        private IEnumerator Ending()
        {
            SetControl(false);
            if (corruptedLight != null)
            {
                corruptedLight.enabled = true;
                corruptedLight.intensity = 1.2f;
            }

            if (endingFragment != null)
                endingFragment.SetActive(true);
            audioManager.PlayArtifact();
            yield return new WaitForSecondsRealtime(0.8f);
            yield return fade.FadeOut(1.2f);
            endingShownAt = Time.unscaledTime;
            Phase = GamePhase.Ending;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void SetControl(bool active)
        {
            player.enabled = active;
            playerCamera.enabled = active;
            interaction.InputEnabled = active;
            realityManager.GameplayInputEnabled = active;
            Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !active;
        }

        private void PlacePlayer(Transform spawn)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            bool enabledBefore = cc.enabled;
            cc.enabled = false;
            player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            player.ResetMotion();
            cc.enabled = enabledBefore;
            Physics.SyncTransforms();
            playerCamera.SnapToTarget(spawn.eulerAngles.y);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        public void Quit()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private bool Button(string text)
        {
            bool pressed = GameUI.Button(text);
            if (pressed)
                audioManager.PlayUI();
            return pressed;
        }

        private void OnGUI()
        {
            Matrix4x4 previous = GameUI.Begin();
            if (titleStyle == null)
            {
                titleStyle = GameUI.Text(40);
                labelStyle = GameUI.Text(12);
                labelStyle.normal.textColor = GameUI.Muted;
            }

            if (Phase == GamePhase.Title || IsPaused || Phase == GamePhase.Ending)
            {
                GameUI.Fill(new Rect(-2000f, -2000f, 5000f, 5000f), new Color(.025f, .03f, .03f, IsPaused ? .85f : .98f));
                GUILayout.BeginArea(new Rect(355f, 72f, 250f, 410f));
                GUILayout.Label(settingsOpen ? "SETTINGS" : IsPaused ? "PAUSED" : "DRIFT", titleStyle);
                GUILayout.Space(12f);
                if (Phase == GamePhase.Ending)
                {
                    float endingAge = Time.unscaledTime - endingShownAt;
                    GUILayout.Label(endingAge >= .6f ? "OUT OF THE DRIFT" : "", labelStyle);
                    GUILayout.Label(endingAge >= 1.5f ? "Thanks for Playing" : "", labelStyle);
                    GUILayout.Space(22f);
                    if (Button("PLAY AGAIN"))
                        PlayAgain();
                    if (Button("MAIN MENU"))
                        MainMenu();
                    if (Button("QUIT"))
                        Quit();
                }
                else if (settingsOpen)
                {
                    GUILayout.Label($"MASTER VOLUME  /  {AudioListener.volume * 100f:0}%", labelStyle);
                    float volume = GameUI.Slider(AudioListener.volume, 0f, 1f);
                    AudioListener.volume = volume;
                    PlayerPrefs.SetFloat("Drift.MasterVolume", volume);
                    GUILayout.Space(10f);
                    GUILayout.Label("MOUSE SENSITIVITY", labelStyle);
                    float sensitivity = GameUI.Slider(playerCamera.MouseSensitivity, 0.02f, 0.5f);
                    playerCamera.MouseSensitivity = sensitivity;
                    PlayerPrefs.SetFloat("Drift.MouseSensitivity", sensitivity);
                    GUILayout.Space(10f);
                    bool subtitles = GameUI.Toggle("Story subtitles", PlayerPrefs.GetInt("Drift.Subtitles", 1) != 0);
                    PlayerPrefs.SetInt("Drift.Subtitles", subtitles ? 1 : 0);
                    bool fullscreen = GameUI.Toggle("Fullscreen", Screen.fullScreen);
                    if (fullscreen != Screen.fullScreen)
                        Screen.fullScreen = fullscreen;
                    if (Button("BACK"))
                    {
                        settingsOpen = false;
                        PlayerPrefs.Save();
                    }
                }
                else if (IsPaused)
                {
                    if (Button("RESUME"))
                        Resume();
                    if (Button("RESTART CHECKPOINT"))
                        RestartCheckpoint();
                    if (Button("SETTINGS"))
                        settingsOpen = true;
                    if (Button("MAIN MENU"))
                        MainMenu();
                    if (Button("QUIT"))
                        Quit();
                }
                else
                {
                    GUILayout.Space(20f);
                    if (Button("PLAY"))
                        Play();
                    if (Button("SETTINGS"))
                        settingsOpen = true;
                    if (Button("QUIT"))
                        Quit();
                }

                GUILayout.EndArea();
            }
            else if (Phase == GamePhase.Opening && Time.unscaledTime < tutorialUntil && !openingSequence.IsTransitioning)
                GUI.Label(new Rect(180f, 20f, 600f, 40f), "WASD — MOVE     SPACE — JUMP     SHIFT — SPRINT     ESC — PAUSE", labelStyle);
            GUI.matrix = previous;
        }
    }
}
