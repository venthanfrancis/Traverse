using System;
using System.Collections;
using UnityEngine;

namespace Drift
{
    // Runs only through the editor test command or explicit --drift-smoke-test argument.
    public sealed class GameJamVerification : MonoBehaviour
    {
        private bool quitWhenDone;
        private int runtimeErrors;
        private void OnEnable() => Application.logMessageReceived += RecordError;
        private void OnDisable() => Application.logMessageReceived -= RecordError;
        private void RecordError(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
                runtimeErrors++;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CommandLineCheck()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--drift-smoke-test") >= 0)
                StartCheck(true);
        }

        public static void StartCheck(bool quit)
        {
            if (FindFirstObjectByType<GameJamVerification>() != null)
                return;
            GameObject runner = new GameObject("JamVerificationRunner");
            DontDestroyOnLoad(runner);
            GameJamVerification check = runner.AddComponent<GameJamVerification>();
            check.quitWhenDone = quit;
            check.StartCoroutine(check.Run());
        }

        private IEnumerator Run()
        {
            IEnumerator routine = Verify();
            while (true)
            {
                object step = null;
                bool more;
                try
                {
                    more = routine.MoveNext();
                    if (more)
                        step = routine.Current;
                }
                catch (Exception exception)
                {
                    Debug.LogError("DRIFT_JAM_SMOKE_FAIL: " + exception);
                    if (quitWhenDone)
                        Application.Quit(1);
                    Destroy(gameObject);
                    yield break;
                }

                if (!more)
                    break;
                yield return step;
            }

            if (runtimeErrors > 0)
            {
                Debug.LogError("DRIFT_JAM_SMOKE_FAIL: runtime errors during verification: " + runtimeErrors);
                if (quitWhenDone) Application.Quit(1);
                Destroy(gameObject);
                yield break;
            }
            Debug.Log("DRIFT_JAM_SMOKE_PASS: title, play, opening lock, artifact, pause/resume, respawn, unordered nodes, portal, cave return, ending, main menu and restart.");
            if (quitWhenDone)
                Application.Quit(0);
            Destroy(gameObject);
        }

        private static void CaptureVfx()
        {
            Camera view = FindFirstObjectByType<ThirdPersonCamera>().GetComponent<Camera>();
            var target = new RenderTexture(1280, 720, 24);
            target.Create();
            var request = new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target };
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(view, request);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../VfxPuzzle.png"), image.EncodeToPNG());
            RenderTexture.active = previous;
            target.Release();
            Destroy(target);
            Destroy(image);
        }

        private static ParticleSystem FindVfx(string name)
        {
            return Array.Find(FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None), item => item.name == name);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
            Debug.Log("Jam verification: " + message + " PASS");
        }

        private static void Place(PlayerMovement player, Vector3 position, float heading = 0f)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, heading, 0f));
            player.ResetMotion();
            controller.enabled = true;
            Physics.SyncTransforms();
            FindFirstObjectByType<ThirdPersonCamera>().SnapToTarget(heading);
        }

        private static IEnumerator WaitForScenes()
        {
            yield return null;
            float deadline = Time.realtimeSinceStartup + 20f;
            while (FindFirstObjectByType<DriftSceneLoader>() != null && !DriftSceneLoader.IsReady)
            {
                if (Time.realtimeSinceStartup > deadline)
                    throw new InvalidOperationException("Scene loading timed out.");
                yield return null;
            }

            yield return null;
        }

        private IEnumerator Verify()
        {
            yield return WaitForScenes();
            GameFlowManager flow = FindFirstObjectByType<GameFlowManager>();
            RealityManager reality = FindFirstObjectByType<RealityManager>();
            OpeningSequence opening = FindFirstObjectByType<OpeningSequence>();
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
            Check(flow.Phase == GameFlowManager.GamePhase.Title && Time.timeScale == 0f && !player.enabled, "title startup blocks gameplay");
            Check(UnityEngine.SceneManagement.SceneManager.sceneCount == 1 && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu", "menu loads no cave or world geometry");
            flow.Play();
            yield return WaitForLevel("Cave");
            var caveMotion = player.GetComponentInChildren<Animator>(true);
            Check(player.GetComponent<PlayerAnimation>() != null && caveMotion != null && caveMotion.runtimeAnimatorController != null && !caveMotion.applyRootMotion, "character has a separate visual animation driver");
            foreach (string state in new[] { "Idle", "Walk", "Run", "Jump", "Fall" })
                Check(caveMotion.HasState(0, Animator.StringToHash("Base Layer." + state)), "character motion clip available: " + state);
            yield return null;
            var subtitles = FindFirstObjectByType<StorySubtitles>();
            var artifactField = typeof(StorySubtitles).GetField("caveArtifacts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Check(((ArtifactInteractable[])artifactField.GetValue(subtitles))?.Length == 2, "subtitle discovers both artifacts after Cave loads");
            yield return VerifyCaveRoute(player);
            Check(FindFirstObjectByType<GameplayVfx>() != null && FindVfx("Ambient dust and ash").isPlaying, "cave has local ambient particles");
            reality.BeginDrift();
            Check(!reality.IsDrifting && !reality.CanDrift, "Drift locked before artifact");
            opening.BeginTransition();
            float arrivalDeadline = Time.realtimeSinceStartup + 20f;
            while (!opening.HasArrived)
            {
                if (Time.realtimeSinceStartup > arrivalDeadline)
                    throw new InvalidOperationException("Artifact arrival timed out.");
                yield return null;
            }

            yield return null;
            Check(opening.IsTransitioning && !player.enabled && !reality.CanDrift && RenderSettings.skybox != null && RenderSettings.skybox.name.Contains("Broken"), "red Broken sky ready during arrival fade, with controls still locked");
            yield return new WaitForSecondsRealtime(.8f);
            Check(opening.HasArrived && reality.CanDrift && player.enabled, "artifact arrival unlocks and restores control");
            Check(FindObjectsByType<GameplayVfx>(FindObjectsSortMode.None).Length == 1, "world replaces cave VFX without persistent duplicates");
            Check(FindVfx("Node motes").GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.isSupported, "VFX particle shader is supported");
            Check(FindVfx("Puzzle energy").GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name == "Drift/SoftParticles", "puzzle energy uses the soft particle material");
            Camera switchingView = FindFirstObjectByType<ThirdPersonCamera>().GetComponent<Camera>();
            float switchingFov = switchingView.fieldOfView;
            reality.BeginDrift();
            yield return new WaitForSeconds(.1f);
            Check(Mathf.Approximately(switchingView.fieldOfView, switchingFov) && FindVfx("Platform arrival") == null, "reality switching has no FOV pulse or platform burst");
            var atmosphereVolume = FindFirstObjectByType<UnityEngine.Rendering.Volume>();
            if (atmosphereVolume != null && atmosphereVolume.profile.TryGet(out UnityEngine.Rendering.Universal.ChromaticAberration aberration))
                Check(aberration.intensity.value == 0f, "reality switching has no chromatic distortion");
            reality.ReturnToBroken();
            Check(UnityEngine.SceneManagement.SceneManager.sceneCount == 1 && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "BrokenWorld", "world arrival unloads both cave and menu scenes");
            VerifySpaceRocks(player, reality);
            yield return new WaitForSeconds(.2f);
            IEnumerator staircase = VerifyStaircase(player, reality);
            while (staircase.MoveNext())
                yield return staircase.Current;
            IEnumerator chamber = VerifyHiddenChamber(player, reality);
            while (chamber.MoveNext())
                yield return chamber.Current;
            IEnumerator observatory = VerifyObservatory(player, reality, interaction);
            while (observatory.MoveNext())
                yield return observatory.Current;
            IEnumerator ridge = VerifyBackRidge(player, reality);
            while (ridge.MoveNext())
                yield return ridge.Current;
            IEnumerator echoPuzzle = VerifyMissingWeight(player, reality, interaction);
            while (echoPuzzle.MoveNext())
                yield return echoPuzzle.Current;
            reality.ArriveInBrokenWorld();
            reality.BeginDrift();
            yield return new WaitForSeconds(.5f);
            float spentCharge = reality.RemainingDriftTime;
            Check(spentCharge < reality.DriftDuration, "Normal consumes stability");
            reality.ReturnToBroken();
            Check(Mathf.Abs(reality.RemainingDriftTime - spentCharge) < .01f, "manual return preserves remaining charge");
            reality.BeginDrift();
            Check(Mathf.Abs(reality.RemainingDriftTime - spentCharge) < .01f, "re-entry does not refill charge");
            reality.ReturnToBroken();
            yield return new WaitForSeconds(.25f);
            Check(reality.RemainingDriftTime > spentCharge && reality.RemainingDriftTime <= reality.DriftDuration, "Broken recharges without exceeding capacity");
            float beforePause = reality.RemainingDriftTime;
            flow.Pause();
            yield return new WaitForSecondsRealtime(0.25f);
            Check(flow.IsPaused && !player.enabled && !interaction.InputEnabled && Mathf.Abs(reality.RemainingDriftTime - beforePause) < 0.01f, "pause freezes controls and recharge");
            flow.Resume();
            yield return null;
            Check(!flow.IsPaused && player.enabled && Time.timeScale == 1f, "resume restores control");
            PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
            float chargeBeforeRespawn = reality.RemainingDriftTime;
            respawn.Respawn();
            Check(!reality.IsDrifting && Mathf.Abs(reality.RemainingDriftTime - chargeBeforeRespawn) < .01f, "respawn returns to Broken without bypassing recharge");
            AnchorController anchor = FindFirstObjectByType<AnchorController>();
            AnchorPortal portal = FindFirstObjectByType<AnchorPortal>();
            Check(!portal.CanInteract && anchor.ActiveNodes == 1 && anchor.IsNodeActive(2), "third node can be restored first while portal remains locked");
            EnergyNode[] nodes = FindObjectsByType<EnergyNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (int i in new[] { 1, 0, 2 })
            {
                EnergyNode node = Array.Find(nodes, item => item.name == "EnergyNode0" + (i + 1));
                if (i < 2)
                    reality.BeginDrift();
                else
                    reality.ReturnToBroken();
                Place(player, node.transform.position + new Vector3(0f, -1.2f, -1.7f));
                yield return null;
                if (i == 2)
                    Check(!node.CanInteract, "restored third node cannot be activated again");
                node.Interact(interaction);
                Check(anchor.IsNodeActive(i) && anchor.ActiveNodes == (i == 1 ? 2 : 3), "energy node " + (i + 1) + " persists in third-second-first activation order");
                Check(!anchor.ActivateNode(i), "duplicate node ignored");
            }

            Check(anchor.IsRestored && flow.Phase == GameFlowManager.GamePhase.FinalEscape, "all nodes start escape exactly once");
            yield return null;
            Light[] lamps = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Check(Array.Exists(lamps, lamp => lamp.name == "ProgressLight" && lamp.intensity >= 5.5f), "Anchor progress brightens tower beacon");
            reality.BeginDrift();
            flow.Pause();
            flow.RestartCheckpoint();
            yield return null;
            Check(flow.Phase == GameFlowManager.GamePhase.FinalEscape && !flow.IsPaused && !reality.IsDrifting && player.enabled && anchor.IsRestored, "checkpoint restart preserves nodes and restores control");
            IEnumerator adventure = VerifyAdventure(player, reality);
            while (adventure.MoveNext())
                yield return adventure.Current;
            Place(player, portal.transform.position + new Vector3(0, -1.2f, -1.7f));
            yield return null;
            Check(portal.CanInteract, "restored portal accepts escape");
            yield return null;
            Check(FindVfx("Portal pull").isPlaying, "escape portal starts inward particles when usable");
            var carried = FindFirstObjectByType<EchoBlockPuzzle>();
            Place(player, carried.transform.position - Vector3.up * .5f);
            carried.Interact(interaction);
            Check(carried.IsHeld, "solved block can be carried before escaping");
            portal.Interact(interaction);
            yield return WaitForLevel("Cave");
            Check(FindFirstObjectByType<EchoBlockPuzzle>() == null, "carried block unloads with BrokenWorld");
            Check(flow.Phase == GameFlowManager.GamePhase.EndingWalk && !reality.CanDrift && player.enabled, "portal returns to cave with Drift locked");
            ArtifactInteractable corrupted = FindObjectsByType<ArtifactInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
            foreach (ArtifactInteractable item in FindObjectsByType<ArtifactInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (item.name == "CorruptedArtifact")
                    corrupted = item;
            Check(!corrupted.CanInteract, "corrupted artifact cannot restart");
            flow.FinishEnding();
            flow.FinishEnding();
            yield return new WaitForSecondsRealtime(2.4f);
            Check(flow.Phase == GameFlowManager.GamePhase.Ending && !player.enabled, "ending triggers once and shows final title");
            flow.MainMenu();
            yield return WaitForScenes();
            flow = FindFirstObjectByType<GameFlowManager>();
            Check(flow.Phase == GameFlowManager.GamePhase.Title && Time.timeScale == 0f, "main menu reload resets session");
            flow.PlayAgain();
            yield return WaitForScenes();
            yield return WaitForLevel("Cave");
            flow = FindFirstObjectByType<GameFlowManager>();
            Check(flow.Phase == GameFlowManager.GamePhase.Opening && Time.timeScale == 1f, "Play Again reloads at cave entrance");
        }

        private static IEnumerator WaitForLevel(string name)
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != name || !DriftSceneLoader.IsReady || !FindFirstObjectByType<PlayerMovement>().enabled)
            {
                if (Time.realtimeSinceStartup > deadline)
                    throw new InvalidOperationException("Level transition timed out: " + name);
                yield return null;
            }
            Check(UnityEngine.SceneManagement.SceneManager.sceneCount == 1, name + " is the only loaded level scene");
        }

        private static IEnumerator VerifyCaveRoute(PlayerMovement player)
        {
            var controller = player.GetComponent<CharacterController>();
            player.enabled = false;
            float distance = 0f;
            foreach (Vector3 destination in new[] { new Vector3(-100, 0, 38.5f), new Vector3(-105.3f, 0, 38.5f), new Vector3(-100, 0, 38.5f), new Vector3(-100, 0, 55), new Vector3(-94.5f, 0, 57.5f), new Vector3(-100, 0, 55), new Vector3(-100, 0, 104), new Vector3(-97, 0, 105.6f) })
            {
                int stalled = 0;
                for (int step = 0; step < 1800; step++)
                {
                    Vector3 offset = Vector3.ProjectOnPlane(destination - player.transform.position, Vector3.up);
                    if (offset.magnitude < .15f) break;
                    Vector3 before = player.transform.position;
                    controller.Move(Vector3.ClampMagnitude(offset, .06f) + Vector3.down * .08f);
                    float moved = Vector3.ProjectOnPlane(player.transform.position - before, Vector3.up).magnitude;
                    distance += moved;
                    stalled = moved < .005f ? stalled + 1 : 0;
                    if (stalled > 35) break;
                    if (step % 20 == 0) yield return null;
                }
                Check(Vector3.ProjectOnPlane(destination - player.transform.position, Vector3.up).magnitude < .2f && player.transform.position.y > -.5f, "cave route reaches " + destination);
            }
            player.enabled = true;
            Debug.Log("DRIFT_CAVE_ROUTE: distance=" + distance.ToString("F1") + "m estimated walk=" + (distance / 4f).ToString("F1") + "s; CharacterController sweep, not a manual playthrough.");
        }

        private static void VerifySpaceRocks(PlayerMovement player, RealityManager reality)
        {
            reality.ReturnToBroken();
            Physics.SyncTransforms();
            MeshFilter[] meshes = FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int rocks = 0;
            MeshCollider sample = null;
            foreach (MeshFilter mesh in meshes)
            {
                if (mesh.gameObject.scene.name != "BrokenWorld" || mesh.sharedMesh == null || !mesh.sharedMesh.name.Contains("FacetedRock"))
                    continue;
                if (mesh.name == "Drifting chip")
                {
                    Check(mesh.GetComponent<Collider>() == null, "VFX chips do not introduce collision");
                    continue;
                }
                if (mesh.name == "StoneChip")
                {
                    Vector3 size = mesh.transform.lossyScale;
                    Check(size.x <= .35f && size.y <= .1f && size.z <= .35f, "decorative chips stay below step height");
                    continue;
                }
                Check(mesh.GetComponent<Collider>() != null && !mesh.GetComponent<Collider>().isTrigger, "physical rock collider: " + mesh.name);
                rocks++;
                if (mesh.name == "FloatingCaveFragment" && mesh.gameObject.activeInHierarchy)
                    sample = mesh.GetComponent<MeshCollider>();
            }

            Check(rocks > 0 && sample != null, "space rocks have solid collision");
            Bounds bounds = sample.bounds;
            Ray ray = new Ray(bounds.center + Vector3.up * (bounds.extents.y + 2), Vector3.down);
            Check(sample.Raycast(ray, out RaycastHit hit, bounds.size.y + 4), "space rock surface can support a landing");
            FloatingObject motion = sample.GetComponent<FloatingObject>();
            if (motion != null)
                motion.enabled = false;
            Place(player, hit.point + Vector3.up * .05f);
            player.enabled = false;
            CharacterController cc = player.GetComponent<CharacterController>();
            for (int i = 0; i < 80; i++)
                cc.Move(Vector3.down * .02f);
            Check(cc.isGrounded && player.transform.position.y >= hit.point.y - .4f, "CharacterController lands on space rock");
            Vector3 position = player.transform.position;
            reality.BeginDrift();
            Physics.SyncTransforms();
            Check(!sample.gameObject.activeInHierarchy && player.transform.position == position, "Broken rock collision deactivates with its world without teleporting");
            reality.ReturnToBroken();
            if (motion != null)
                motion.enabled = true;
            player.enabled = true;
            player.GetComponent<PlayerRespawn>().Respawn();
            int missingComponents = 0;
            foreach (Transform item in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                foreach (Component component in item.GetComponents<Component>())
                    if (component == null)
                        missingComponents++;
            Check(missingComponents == 0, "loaded scenes have no missing scripts");
            int activeCameras = 0;
            foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (camera.isActiveAndEnabled && camera.targetTexture == null && camera.targetDisplay == 0)
                {
                    activeCameras++;
                    Check(camera.GetComponent<ThirdPersonCamera>() != null, "game view uses the third-person camera");
                }
            Check(activeCameras == 1, "one camera renders the game view");
            bool visibleLines = false;
            foreach (LineRenderer line in FindObjectsByType<LineRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (line.enabled) visibleLines = true;
            Check(!visibleLines, "decorative environment lines stay hidden");
        }

        private static IEnumerator VerifyHiddenChamber(PlayerMovement player, RealityManager reality)
        {
            reality.BeginDrift();
            Physics.SyncTransforms();
            Check(Physics.Raycast(new Vector3(74, 1, 27), Vector3.left, 3, ~0, QueryTriggerInteraction.Ignore), "intact doorway blocks Normal entrance");
            reality.ReturnToBroken();
            Physics.SyncTransforms();
            Check(!Physics.Raycast(new Vector3(74, 1, 27), Vector3.left, 3, ~0, QueryTriggerInteraction.Ignore), "Broken doorway reveals chamber route");
            Check(!Physics.Raycast(new Vector3(63, 2, 27), Vector3.down, 3, ~0, QueryTriggerInteraction.Ignore), "chamber bridge has no hidden Broken collider");
            Place(player, new Vector3(68.5f, .05f, 27), -90);
            yield return new WaitForSeconds(.1f);
            Check(player.GetComponent<PlayerRespawn>().CurrentCheckpoint.name == "ChamberCheckpointSpawn", "hidden route records safe checkpoint");
            reality.BeginDrift();
            Physics.SyncTransforms();
            Check(Physics.Raycast(new Vector3(63, 2, 27), Vector3.down, 3, ~0, QueryTriggerInteraction.Ignore), "Normal restores chamber bridge");
            player.enabled = false;
            CharacterController cc = player.GetComponent<CharacterController>();
            for (int i = 0; i < 290; i++)
                cc.Move(new Vector3(-.04f, -.02f, 0));
            Check(player.transform.position.x < 58 && player.transform.position.y > -.1f, "CharacterController crosses restored chamber bridge");
            player.enabled = true;
            reality.ReturnToBroken();
            Physics.SyncTransforms();
            Check(Physics.Raycast(new Vector3(57, 2, 27), Vector3.down, 3, ~0, QueryTriggerInteraction.Ignore), "chamber remains safe after reality returns");
            Place(player, new Vector3(56, .05f, 27));
            yield return new WaitForSeconds(.15f);
            Check(!Physics.Raycast(new Vector3(60, 1, 36), Vector3.right, 4, ~0, QueryTriggerInteraction.Ignore), "shortcut gate loses collision");
            reality.BeginDrift();
            foreach (bool normal in new[] { false, true })
            {
                if (normal)
                    reality.BeginDrift();
                else
                    reality.ReturnToBroken();
                Physics.SyncTransforms();
                Place(player, new Vector3(56, .05f, 27));
                player.enabled = false;
                for (int i = 0; i < 225; i++)
                    cc.Move(new Vector3(0, -.02f, .04f));
                for (int i = 0; i < 560; i++)
                    cc.Move(new Vector3(.04f, -.02f, 0));
                Check(player.transform.position.x > 77 && player.transform.position.y > -.1f, "chamber shortcut walk in " + reality.CurrentDimension + " at " + player.transform.position);
                player.enabled = true;
            }
            player.GetComponent<PlayerRespawn>().Respawn();
            yield return null;
        }

        private static IEnumerator VerifyObservatory(PlayerMovement player, RealityManager reality, PlayerInteraction interaction)
        {
            ObservatoryTelescope telescope = FindFirstObjectByType<ObservatoryTelescope>(FindObjectsInactive.Include);
            Check(telescope != null && !telescope.Activated, "optional observatory telescope exists");
            reality.ReturnToBroken();
            Physics.SyncTransforms();
            Check(!Physics.Raycast(new Vector3(78, 4, 48), Vector3.down, 5, ~0, QueryTriggerInteraction.Ignore), "Broken observatory stairs have no hidden colliders");
            reality.BeginDrift();
            Place(player, new Vector3(78, .05f, 42.5f));
            player.enabled = false;
            CharacterController cc = player.GetComponent<CharacterController>();
            for (int i = 0; i < 270; i++)
                cc.Move(new Vector3(0, -.02f, .04f));
            Check(player.transform.position.z > 52 && player.transform.position.y > 2.9f, "CharacterController climbs restored observatory stairs");
            player.enabled = true;
            yield return null;
            telescope.Interact(interaction);
            telescope.Interact(interaction);
            Check(telescope.Activated && !telescope.CanInteract, "telescope discovery activates only once");
            reality.ReturnToBroken();
            Physics.SyncTransforms();
            Check(Physics.Raycast(new Vector3(78, 5, 53), Vector3.down, 3, ~0, QueryTriggerInteraction.Ignore), "observatory balcony stays safe in Broken");
            Check(!Physics.Raycast(new Vector3(82, 4, 55), Vector3.right, 5, ~0, QueryTriggerInteraction.Ignore), "telescope opens Broken shortcut seal");
            Place(player, new Vector3(90, 3.05f, 55));
            player.enabled = false;
            for (int i = 0; i < 330; i++)
                cc.Move(new Vector3(0, -.04f, -.04f));
            Check(player.transform.position.z < 44 && player.transform.position.y < .3f, "Broken shortcut descends to courtyard landing");
            player.enabled = true;
            player.GetComponent<PlayerRespawn>().Respawn();
            yield return null;
            Check(telescope.Activated, "checkpoint retains optional discovery");
        }

        private static IEnumerator VerifyBackRidge(PlayerMovement player, RealityManager reality)
        {
            reality.ReturnToBroken();
            Physics.SyncTransforms();
            Check(!Physics.Raycast(new Vector3(100, 3, 210), Vector3.down, 4, ~0, QueryTriggerInteraction.Ignore), "ridge stairs have no hidden Broken support");
            reality.BeginDrift();
            Place(player, new Vector3(100, .85f, 206));
            player.enabled = false;
            CharacterController cc = player.GetComponent<CharacterController>();
            for (int i = 0; i < 290; i++)
                cc.Move(new Vector3(0, -.02f, .04f));
            Check(player.transform.position.z > 217 && player.transform.position.y > 3.7f, "CharacterController climbs restored ridge stairs");
            player.enabled = true;
            yield return new WaitForSeconds(.1f);
            Check(player.GetComponent<PlayerRespawn>().CurrentCheckpoint.name == "RidgeRestCheckpointSpawn", "ridge resting ledge saves checkpoint");
            Check(!Physics.Raycast(new Vector3(100, 7, 226), Vector3.down, 4, ~0, QueryTriggerInteraction.Ignore), "jump rocks disappear in Normal");
            reality.ReturnToBroken();
            Physics.SyncTransforms();
            Check(Physics.Raycast(new Vector3(100, 6, 217.5f), Vector3.down, 4, ~0, QueryTriggerInteraction.Ignore), "ridge ledge remains safe after switching");
            float[] startZ = { 219, 222.8f, 226.8f, 230.8f };
            float[] startY = { 3.85f, 4.45f, 5.05f, 5.65f };
            for (int hop = 0; hop < 4; hop++)
            {
                Place(player, new Vector3(100, startY[hop], startZ[hop]));
                player.enabled = false;
                float velocity = Mathf.Sqrt(2f * 25f * 1.5f);
                bool landed = false;
                for (int frame = 0; frame < 80; frame++)
                {
                    cc.Move(new Vector3(0, velocity * .02f, .1f));
                    velocity -= 25f * .02f;
                    if (frame > 12 && velocity < 0 && cc.isGrounded)
                    {
                        landed = true;
                        break;
                    }
                }
                Check(landed && player.transform.position.z > startZ[hop] + 2, "ridge jump " + (hop + 1) + " lands on solid support");
                player.enabled = true;
            }
            Place(player, new Vector3(100, 6.25f, 234));
            yield return new WaitForSeconds(.15f);
            Transform gate = Array.Find(FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None), item => item.name == "RidgeShortcutGate");
            Check(gate.gameObject.activeInHierarchy, "left descent door stays locked before the echo solution");
            Check(Physics.Raycast(new Vector3(109, 2, 209), Vector3.back, 4, ~0, QueryTriggerInteraction.Ignore), "left door has solid collision before solve");
        }

        private static IEnumerator VerifyRidgeReturn(PlayerMovement player, RealityManager reality)
        {
            yield return null;
            Transform gate = Array.Find(FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None), item => item.name == "RidgeShortcutGate");
            CharacterController cc = player.GetComponent<CharacterController>();
            foreach (bool normal in new[] { false, true })
            {
                if (normal) reality.BeginDrift(); else reality.ReturnToBroken();
                Physics.SyncTransforms();
                Check(!gate.gameObject.activeInHierarchy, "solved left door stays open in " + reality.CurrentDimension);
                Check(!Physics.Raycast(new Vector3(109, 2, 209), Vector3.back, 4, ~0, QueryTriggerInteraction.Ignore), "left door loses collision in " + reality.CurrentDimension);
            }
            reality.ReturnToBroken();
            Place(player, new Vector3(109, 6.25f, 234));
            player.enabled = false;
            for (int i = 0; i < 675; i++)
                cc.Move(new Vector3(0, -.08f, -.04f));
            Check(player.transform.position.z < 208 && player.transform.position.y > .7f, "ridge shortcut descends to base");
            for (int i = 0; i < 225; i++)
                cc.Move(new Vector3(-.04f, -.02f, 0));
            for (int i = 0; i < 100; i++)
                cc.Move(new Vector3(0, -.02f, -.04f));
            Check(player.transform.position.x < 101 && player.transform.position.z < 205, "ridge loop returns to original landing at " + player.transform.position);
            player.enabled = true;

        }

        private static IEnumerator VerifyMissingWeight(PlayerMovement player, RealityManager reality, PlayerInteraction interaction)
        {
            EchoBlockPuzzle puzzle = FindFirstObjectByType<EchoBlockPuzzle>(FindObjectsInactive.Include);
            Check(puzzle != null && !puzzle.IsSolved, "Missing Weight starts with one block and a locked gate");
            Transform gate = Array.Find(FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None), item => item.name == "MissingWeightGate");
            Check(!gate.GetComponent<Renderer>().enabled && gate.Find("GateBars") != null, "barred gate reveals its reward while retaining collision");
            Light firstSocket = Array.Find(FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None), item => item.name == "GateLight1");
            Light secondSocket = Array.Find(FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None), item => item.name == "GateLight2");
            AnchorController anchor = FindFirstObjectByType<AnchorController>();
            EnergyNode[] nodes = FindObjectsByType<EnergyNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            EnergyNode finalNode = Array.Find(nodes, node => node.name == "EnergyNode03");
            Check(Vector3.Distance(finalNode.transform.position, new Vector3(78, 7.5f, 234.5f)) < .1f, "third node is beyond the echo gate");
            reality.BeginDrift();
            for (int i = 0; i < 2; i++)
            {
                EnergyNode node = Array.Find(nodes, item => item.name == "EnergyNode0" + (i + 1));
                Place(player, node.transform.position + new Vector3(0, -1.2f, -1.7f));
                yield return null;
                Check(node.CanInteract && anchor.ActiveNodes == 0, "node " + (i + 1) + " is available without restoring earlier anchors");
            }
            reality.ReturnToBroken();
            finalNode.Interact(interaction);
            Check(anchor.ActiveNodes == 0 && !finalNode.CanInteract, "unsolved echo prevents third node activation independently of anchor order");
            reality.ReturnToBroken();
            Place(player, new Vector3(90, 6.25f, 233.4f));
            yield return new WaitForSeconds(.1f);
            Check(interaction.Nearby == puzzle, "block is reachable through existing E interaction");
            Check(player.GetComponent<PlayerRespawn>().CurrentCheckpoint.name == "EchoRoomCheckpointSpawn", "puzzle entrance saves a silent local retry checkpoint");
            Check(Physics.Raycast(new Vector3(84, 7.2f, 234.5f), Vector3.left, 5, ~0, QueryTriggerInteraction.Ignore), "unsolved barred gate blocks passage");
            puzzle.Interact(interaction);
            Check(puzzle.IsHeld, "block pickup works");
            Check(puzzle.gameObject.scene.name == "BrokenWorld" && !puzzle.transform.IsChildOf(player.transform), "carried block stays in its level scene");
            puzzle.ReleaseCarry();
            Check(!puzzle.IsHeld && !puzzle.GetComponent<Collider>().isTrigger, "carry cleanup restores a solid block");
            puzzle.Interact(interaction);
            Place(player, new Vector3(86, 6.25f, 230.8f));
            yield return null;
            puzzle.Interact(interaction);
            Check(!puzzle.HasEcho && !puzzle.IsHeld, "Broken placement creates no echo");
            puzzle.Interact(interaction);
            reality.BeginDrift();
            Place(player, new Vector3(86, 6.25f, 230.8f));
            yield return null;
            puzzle.Interact(interaction);
            Vector3 recorded = puzzle.transform.position;
            Check(puzzle.HasEcho && puzzle.EchoPosition == recorded, "Normal placement records echo on first plate");
            puzzle.Interact(interaction);
            reality.ReturnToBroken();
            Place(player, new Vector3(86, 6.25f, 235.8f));
            yield return new WaitForSeconds(.1f);
            Check(!puzzle.IsSolved, "player and carried block cannot substitute for a second weight");
            reality.BeginDrift();
            Check(!puzzle.HasEcho, "a new Normal visit lets player correct the recorded arrangement");
            Place(player, new Vector3(86, 6.25f, 230.8f));
            yield return null;
            puzzle.Interact(interaction);
            puzzle.Interact(interaction);
            Place(player, new Vector3(86, 6.25f, 235.8f));
            yield return null;
            puzzle.Interact(interaction);
            yield return new WaitForSeconds(.1f);
            Check(puzzle.EchoPosition == recorded && Vector3.Distance(puzzle.transform.position, recorded) > 4, "moving real block leaves the recorded echo behind");
            Check(!puzzle.IsSolved, "Normal echo preview does not press a second plate");
            Check(firstSocket.intensity < .2f && secondSocket.intensity > 1, "gate sockets distinguish preview echo from real plate weight");
            reality.ReturnToBroken();
            yield return new WaitForSeconds(.1f);
            Check(puzzle.IsSolved, "Broken echo and real block solve two-plate gate");
            Check(FindVfx("Puzzle energy").particleCount > 0, "echo solution sends visible energy toward the gate");
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--drift-vfx-capture") >= 0)
            {
                FindFirstObjectByType<ThirdPersonCamera>().SnapToTarget(-90f);
                yield return new WaitForSeconds(.15f);
                CaptureVfx();
                yield return new WaitForSeconds(.2f);
            }
            Check(firstSocket.intensity > 1 && secondSocket.intensity > 1, "both powered sockets confirm the echo solution");
            Place(player, finalNode.transform.position + new Vector3(0, -1.2f, -1.7f));
            yield return null;
            Check(finalNode.CanInteract, "solved puzzle makes third node available with no other anchors active");
            finalNode.Interact(interaction);
            Check(anchor.ActiveNodes == 1 && anchor.IsNodeActive(2) && !anchor.IsRestored, "interacting restores third anchor first without starting escape");
            Check(!anchor.ActivateNode(2) && !anchor.ActivateNode(-1) && !anchor.ActivateNode(3), "duplicate and invalid anchor indices do not change progress");
            Check(!Physics.Raycast(new Vector3(84, 7.2f, 234.5f), Vector3.left, 5, ~0, QueryTriggerInteraction.Ignore), "solved echo gate no longer blocks passage");
            CharacterController cc = player.GetComponent<CharacterController>();
            foreach (bool normal in new[] { false, true })
            {
                if (normal)
                    reality.BeginDrift();
                else
                    reality.ReturnToBroken();
                Place(player, new Vector3(86, 6.25f, 234.5f), -90);
                player.enabled = false;
                for (int i = 0; i < 140; i++)
                    cc.Move(new Vector3(-.04f, -.02f, 0));
                Check(player.transform.position.x < 81 && player.transform.position.y > 6.1f, "echo lookout is reachable in " + reality.CurrentDimension);
                player.enabled = true;
            }
            player.GetComponent<PlayerRespawn>().Respawn();
            yield return null;
            Check(puzzle.IsSolved, "Missing Weight solution survives checkpoint retry");
            IEnumerator returnRoute = VerifyRidgeReturn(player, reality);
            while (returnRoute.MoveNext())
                yield return returnRoute.Current;
        }

        private static IEnumerator VerifyAdventure(PlayerMovement player, RealityManager reality)
        {
            AdventureCollapse collapse = FindFirstObjectByType<AdventureCollapse>(FindObjectsInactive.Include);
            Check(collapse != null, "adventure passage exists");
            reality.ReturnToBroken();
            Physics.SyncTransforms();
            Check(!Physics.Raycast(new Vector3(88, 2, 27), Vector3.down, 3, ~0, QueryTriggerInteraction.Ignore), "hidden route absent in Broken");
            reality.BeginDrift();
            Physics.SyncTransforms();
            Check(Physics.Raycast(new Vector3(88, 2, 27), Vector3.down, 3, ~0, QueryTriggerInteraction.Ignore), "hidden route supports player in Normal");
            Place(player, new Vector3(78, .05f, 27));
            yield return new WaitForSeconds(.1f);
            reality.ReturnToBroken();
            Physics.SyncTransforms();
            Check(Physics.Raycast(new Vector3(78, 2, 27), Vector3.down, 3, ~0, QueryTriggerInteraction.Ignore), "overlook remains safe after expiry");
            Place(player, new Vector3(140, .9f, 90));
            yield return new WaitForSeconds(.1f);
            Check(collapse.IsRunning, "restored Anchor arms collapsing passage on entry");
            Place(player, new Vector3(168, .9f, 83));
            yield return new WaitForSeconds(4.1f);
            Physics.SyncTransforms();
            Check(!Physics.Raycast(new Vector3(140, 2, 88), Vector3.down, 3, ~0, QueryTriggerInteraction.Ignore), "collapsed slab loses support in Broken");
            reality.BeginDrift();
            Physics.SyncTransforms();
            Check(!Physics.Raycast(new Vector3(140, 2, 88), Vector3.down, 3, ~0, QueryTriggerInteraction.Ignore), "switching cannot restore collapsed slab");
            collapse.Restore();
            reality.ReturnToBroken();
            Physics.SyncTransforms();
            Check(Physics.Raycast(new Vector3(140, 2, 88), Vector3.down, 3, ~0, QueryTriggerInteraction.Ignore), "passage restores for retry");
            Place(player, new Vector3(168, .85f, 86.5f));
            player.enabled = false;
            CharacterController cc = player.GetComponent<CharacterController>();
            for (int i = 0; i < 550; i++)
                cc.Move(new Vector3(0, -.02f, .04f));
            Check(player.transform.position.z > 107 && player.transform.position.y > 6.7f, "CharacterController climbs all tower steps");
            player.enabled = true;
            yield return new WaitForSeconds(.1f);
            Check(FindFirstObjectByType<AdventureFeedback>().SummitReached, "summit arrival response triggers after restoration");
        }

        private static IEnumerator VerifyStaircase(PlayerMovement player, RealityManager reality)
        {
            FallingStaircase puzzle = FindFirstObjectByType<FallingStaircase>(FindObjectsInactive.Include);
            Check(puzzle != null, "Falling Staircase scene setup exists");
            reality.ReturnToBroken();
            Physics.SyncTransforms();
            Check(!Physics.Raycast(new Vector3(114, 4, 29), Vector3.down, 5, ~0, QueryTriggerInteraction.Ignore), "Broken upper landing and collider absent");
            Check(Physics.Raycast(new Vector3(100, 1, 32), Vector3.forward, 2, ~0, QueryTriggerInteraction.Ignore), "unsolved staircase seal blocks passage");
            reality.BeginDrift();
            Physics.SyncTransforms();
            Check(Physics.Raycast(new Vector3(115, 4, 29), Vector3.down, 3, ~0, QueryTriggerInteraction.Ignore), "Normal upper landing supports player");
            Place(player, new Vector3(105, .05f, 29), 90);
            CharacterController cc = player.GetComponent<CharacterController>();
            player.enabled = false;
            // Exercise the actual CharacterController against every riser, including the return route.
            for (int i = 0; i < 250; i++)
                cc.Move(new Vector3(.04f, -.02f, 0));
            Debug.Log("Staircase climb position: " + player.transform.position);
            Check(player.transform.position.x > 113.5f && player.transform.position.y > 2.4f, "CharacterController climbs restored stairs without movement changes");
            Vector3 before = player.transform.position;
            reality.ReturnToBroken();
            Physics.SyncTransforms();
            Check(player.transform.position == before, "staircase switch preserves player position");
            player.enabled = true;
            float deadline = Time.realtimeSinceStartup + 4;
            while (!puzzle.IsSolved && Time.realtimeSinceStartup < deadline)
                yield return null;
            Check(puzzle.IsSolved && player.transform.position.y < -2.8f, "existing player gravity lands on cyan target and opens seal");
            Check(!Physics.Raycast(new Vector3(100, 1, 32), Vector3.forward, 2, ~0, QueryTriggerInteraction.Ignore), "Broken seal collider removed");
            reality.BeginDrift();
            Physics.SyncTransforms();
            Check(!Physics.Raycast(new Vector3(100, 1, 32), Vector3.forward, 2, ~0, QueryTriggerInteraction.Ignore), "Normal seal remains open after switching");
            reality.ReturnToBroken();
            Place(player, new Vector3(114.5f, -2.95f, 38), 270);
            player.enabled = false;
            for (int i = 0; i < 260; i++)
                cc.Move(new Vector3(-.04f, -.02f, 0));
            Debug.Log("Staircase return position: " + player.transform.position);
            Check(player.transform.position.x < 104.5f && player.transform.position.y > -.1f, "CharacterController exits lower passage through return stairs");
            player.enabled = true;
            player.GetComponent<PlayerRespawn>().Respawn();
            yield return null;
            Check(puzzle.IsSolved, "checkpoint respawn preserves staircase completion");
        }
    }
}
