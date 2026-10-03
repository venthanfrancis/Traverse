using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Beat = NarrationSequence.NarrationBeats;

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
        public bool IsGameplayRunning => !IsPaused && Phase != GamePhase.Title && Phase != GamePhase.Ending && Phase != GamePhase.Returning && !openingSequence.IsTransitioning;

        private bool settingsOpen;
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
            settingsOpen = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            tutorialUntil = Time.unscaledTime + 9f;
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
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void PlayAgain()
        {
            playAfterReload = true;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void BeginEscape()
        {
            if (Phase == GamePhase.FinalEscape || Phase == GamePhase.Returning || Phase == GamePhase.EndingWalk || Phase == GamePhase.Ending)
                return;
            Phase = GamePhase.FinalEscape;
            audioManager.PlayAnchor();
            NarrationManager.Announce(Beat.EscapeStart);
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
            realityManager.ArriveInBrokenWorld();
            brokenWorld.SetActive(false);
            normalWorld.SetActive(false);
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
            NarrationManager.Announce(Beat.ReturnHome, 1f);
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
            bool pressed = GUILayout.Button(text, GUILayout.Height(42f));
            if (pressed)
                audioManager.PlayUI();
            return pressed;
        }

        private void OnGUI()
        {
            Matrix4x4 previous = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 960f, Screen.height / 540f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 960f * scale) * 0.5f, (Screen.height - 540f * scale) * 0.5f), Quaternion.identity, Vector3.one * scale);
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 52,
                    alignment = TextAnchor.MiddleCenter
                };
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
            }

            if (Phase == GamePhase.Title || IsPaused || Phase == GamePhase.Ending)
            {
                GUI.color = new Color(0.035f, 0.045f, 0.075f, 0.97f);
                GUI.DrawTexture(new Rect(-2000f, -2000f, 5000f, 5000f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUILayout.BeginArea(new Rect(310f, 80f, 340f, 400f));
                GUILayout.Label(IsPaused ? "PAUSED" : "DRIFT", titleStyle);
                if (Phase == GamePhase.Ending)
                {
                    float endingAge = Time.unscaledTime - endingShownAt;
                    GUILayout.Label(endingAge >= .6f ? "THE DRIFT HAS BEGUN" : "", labelStyle);
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
                    GUILayout.Label("MASTER VOLUME", labelStyle);
                    float volume = GUILayout.HorizontalSlider(AudioListener.volume, 0f, 1f);
                    AudioListener.volume = volume;
                    PlayerPrefs.SetFloat("Drift.MasterVolume", volume);
                    GUILayout.Space(14f);
                    GUILayout.Label("MOUSE SENSITIVITY", labelStyle);
                    float sensitivity = GUILayout.HorizontalSlider(playerCamera.MouseSensitivity, 0.02f, 0.5f);
                    playerCamera.MouseSensitivity = sensitivity;
                    PlayerPrefs.SetFloat("Drift.MouseSensitivity", sensitivity);
                    GUILayout.Space(14f);
                    bool fullscreen = GUILayout.Toggle(Screen.fullScreen, " Fullscreen");
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
            GUI.matrix = previous;
            if (Phase == GamePhase.Opening && !IsPaused && Time.unscaledTime < tutorialUntil && !openingSequence.IsTransitioning)
            {
                float width = Mathf.Min(Hud.Px(1180f), Screen.width - 24f);
                Hud.Box(new Rect((Screen.width - width) * 0.5f, Hud.Px(148f), width, Hud.Px(72f)), "WASD — MOVE     SPACE — JUMP     SHIFT — SPRINT     ESC — PAUSE", 32f);
            }
        }
    }
}
