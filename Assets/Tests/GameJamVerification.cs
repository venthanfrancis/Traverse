using System;
using System.Collections;
using UnityEngine;

namespace Drift
{
    // Runs only through the editor test command or explicit --drift-smoke-test argument.
    public sealed class GameJamVerification : MonoBehaviour
    {
        private bool quitWhenDone;
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

            Debug.Log("DRIFT_JAM_SMOKE_PASS: title, play, opening lock, artifact, pause/resume, respawn, ordered nodes, portal, cave return, ending, main menu and restart.");
            if (quitWhenDone)
                Application.Quit(0);
            Destroy(gameObject);
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
            flow.Play();
            yield return null;
            reality.BeginDrift();
            Check(!reality.IsDrifting && !reality.CanDrift, "Drift locked before artifact");
            opening.BeginTransition();
            float arrivalDeadline = Time.realtimeSinceStartup + 5f;
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
            VerifySpaceRocks(player, reality);
            yield return new WaitForSeconds(.2f);
            IEnumerator staircase = VerifyStaircase(player, reality);
            while (staircase.MoveNext())
                yield return staircase.Current;
            IEnumerator observatory = VerifyObservatory(player, reality, interaction);
            while (observatory.MoveNext())
                yield return observatory.Current;
            reality.BeginDrift();
            float beforePause = reality.RemainingDriftTime;
            flow.Pause();
            yield return new WaitForSecondsRealtime(0.25f);
            Check(flow.IsPaused && !player.enabled && !interaction.InputEnabled && Mathf.Abs(reality.RemainingDriftTime - beforePause) < 0.01f, "pause freezes controls and Drift timer");
            flow.Resume();
            yield return null;
            Check(!flow.IsPaused && player.enabled && Time.timeScale == 1f, "resume restores control");
            PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
            respawn.Respawn();
            Check(!reality.IsDrifting && reality.RemainingDriftTime == reality.DriftDuration, "respawn resets Broken state and full timer");
            AnchorController anchor = FindFirstObjectByType<AnchorController>();
            AnchorPortal portal = FindFirstObjectByType<AnchorPortal>();
            Check(!portal.CanInteract && !anchor.ActivateNode(1), "Anchor rejects incomplete and out-of-order activation");
            EnergyNode[] nodes = FindObjectsByType<EnergyNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < 3; i++)
            {
                EnergyNode node = Array.Find(nodes, item => item.name == "EnergyNode0" + (i + 1));
                if (i < 2)
                    reality.BeginDrift();
                else
                    reality.ReturnToBroken();
                Place(player, node.transform.position + new Vector3(0f, -1.2f, -1.7f));
                yield return null;
                node.Interact(interaction);
                Check(anchor.ActiveNodes == i + 1, "energy node " + (i + 1) + " persists");
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
            portal.Interact(interaction);
            yield return new WaitForSecondsRealtime(2.3f);
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
            flow = FindFirstObjectByType<GameFlowManager>();
            Check(flow.Phase == GameFlowManager.GamePhase.Opening && Time.timeScale == 1f, "Play Again reloads at cave entrance");
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
            bool visibleLines = false;
            foreach (LineRenderer line in FindObjectsByType<LineRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (line.enabled) visibleLines = true;
            Check(!visibleLines, "decorative environment lines stay hidden");
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
