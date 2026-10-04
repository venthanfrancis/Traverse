using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Drift
{
    // Created after level references are bound; destroyed with that level.
    public sealed class GameplayVfx : MonoBehaviour
    {
        private readonly List<Transform> fragments = new List<Transform>();
        private readonly List<Vector3> fragmentHomes = new List<Vector3>();
        private RealityManager reality;
        private GameFlowManager flow;
        private Transform player;
        private ParticleSystem atmosphere, energy, portalParticles;
        private AnchorPortal portal;
        private EchoBlockPuzzle puzzle;
        private Transform[] plates;
        private Transform gate;
        private Transform shortcut;
        private Material particles;
        private bool solved;
        private readonly Color cyan = new Color(.25f, .9f, 1f, .65f);
        private readonly Color powered = new Color(.35f, 1.4f, 1.6f, .85f);

        private void Start()
        {
            reality = FindFirstObjectByType<RealityManager>();
            flow = FindFirstObjectByType<GameFlowManager>();
            var movement = FindFirstObjectByType<PlayerMovement>();
            if (reality == null || flow == null || movement == null)
            {
                enabled = false;
                return;
            }
            player = movement.transform;
            particles = Resources.Load<Material>("DriftParticles");
            if (particles == null)
            {
                Debug.LogError("Missing DriftParticles material.", this);
                enabled = false;
                return;
            }
            atmosphere = CreateParticles("Ambient dust and ash", 90, 7f, .035f);
            var shape = atmosphere.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(16f, 8f, 16f);
            atmosphere.Play();
            energy = CreateParticles("Puzzle energy", 100, .7f, .13f);
            var energyMain = energy.main;
            energyMain.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            foreach (Renderer item in GetComponentsInChildren<Renderer>(true))
            {
                if (item is ParticleSystemRenderer)
                    continue;
                string itemName = item.name;
                if (itemName == "FloatingCaveFragment" && fragments.Count < 18)
                    CreateFragment(item);
            }
            foreach (EnergyNode node in GetComponentsInChildren<EnergyNode>(true))
                AddOrbit(node.transform, "Node motes", .6f);
            foreach (ArtifactInteractable artifact in GetComponentsInChildren<ArtifactInteractable>(true))
                AddOrbit(artifact.transform, "Artifact orbit", .7f);

            puzzle = GetComponentInChildren<EchoBlockPuzzle>(true);
            if (puzzle != null)
            {
                var found = new List<Transform>();
                foreach (Transform item in GetComponentsInChildren<Transform>(true))
                {
                    if (item.name == "WeightPlate01" || item.name == "WeightPlate02") found.Add(item);
                    if (item.name == "MissingWeightGate") gate = item;
                    if (item.name == "RidgeShortcutGate") shortcut = item;
                }
                plates = found.ToArray();
                solved = puzzle.IsSolved;
            }
            portal = GetComponentInChildren<AnchorPortal>(true);
            if (portal != null)
            {
                portalParticles = CreateParticles("Portal pull", 70, 1.5f, .065f);
                portalParticles.transform.SetParent(portal.transform, false);
                var portalShape = portalParticles.shape;
                portalShape.enabled = true;
                portalShape.shapeType = ParticleSystemShapeType.Sphere;
                portalShape.radius = 1.8f;
                portalShape.radiusThickness = 0f;
                var main = portalParticles.main;
                main.startSpeed = -1.1f;
                var emission = portalParticles.emission;
                emission.rateOverTime = 22f;
            }
            reality.DimensionChanged += WorldChanged;
            SetAtmosphere();
        }

        private ParticleSystem CreateParticles(string label, int budget, float lifetime, float size)
        {
            var item = new GameObject(label);
            item.transform.SetParent(transform, false);
            var system = item.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = true;
            main.maxParticles = budget;
            main.startLifetime = lifetime;
            main.startSize = new ParticleSystem.MinMaxCurve(size * .5f, size);
            main.startSpeed = .08f;
            main.startColor = cyan;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.PauseAndCatchup;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.enabled = false;
            var fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .15f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = item.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particles;
            SetParticleStreams(renderer);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            system.Play();
            return system;
        }

        private static void SetParticleStreams(ParticleSystemRenderer renderer)
        {
            renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV
            });
        }

        private void AddOrbit(Transform target, string label, float radius)
        {
            var system = CreateParticles(label, 24, 2f, .045f);
            system.transform.SetParent(target, false);
            var main = system.main;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startSpeed = 0f;
            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = 0f;
            system.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.orbitalZ = .8f;
            var emission = system.emission;
            emission.rateOverTime = 5f;
            if (system.gameObject.activeInHierarchy) system.Play();
        }

        private void CreateFragment(Renderer rock)
        {
            var mesh = rock.GetComponent<MeshFilter>();
            if (mesh == null || mesh.sharedMesh == null || rock.bounds.size.magnitude < 3f)
                return;
            var fragment = new GameObject("Drifting chip");
            fragment.transform.SetParent(rock.transform, false);
            fragment.transform.position = rock.bounds.center + Vector3.right * (rock.bounds.extents.x + .4f);
            fragment.transform.rotation = Quaternion.Euler(23f, 51f, 17f);
            fragment.transform.localScale = Vector3.one * .045f;
            fragment.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
            var renderer = fragment.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = rock.sharedMaterials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fragments.Add(fragment.transform);
            fragmentHomes.Add(fragment.transform.localPosition);
        }

        private void WorldChanged(RealityManager.Dimension dimension)
        {
            SetAtmosphere();
        }

        private void SetAtmosphere()
        {
            bool cave = gameObject.scene.name == "Cave";
            bool normal = reality.IsDrifting;
            var main = atmosphere.main;
            main.startColor = cave ? new Color(.8f, .65f, .4f, .25f) : normal ? new Color(.5f, .8f, .75f, .2f) : new Color(.9f, .35f, .18f, .3f);
            var emission = atmosphere.emission;
            emission.rateOverTime = cave ? 8f : 6f;
            var velocity = atmosphere.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = cave || normal ? .025f : .12f;
        }

        private void Update()
        {
            if (player == null || flow.IsPaused) return;
            atmosphere.transform.position = player.position + Vector3.up * 2f;
            for (int i = 0; i < fragments.Count; i++)
            {
                if (fragments[i] == null || !fragments[i].gameObject.activeInHierarchy) continue;
                fragments[i].localPosition = fragmentHomes[i] + Vector3.up * Mathf.Sin(Time.time * .6f + i) * .02f;
                fragments[i].Rotate(0f, 9f * Time.deltaTime, 0f, Space.Self);
            }
            if (portalParticles != null)
            {
                if (portal.CanInteract && !portalParticles.isPlaying) portalParticles.Play();
                else if (!portal.CanInteract && portalParticles.isPlaying) portalParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (puzzle != null && puzzle.IsSolved && !solved)
            {
                solved = true;
                if (gate != null && plates.Length > 0) StartCoroutine(PowerDoor());
            }
        }

        private IEnumerator PowerDoor()
        {
            Vector3 destination = gate.position + Vector3.up;
            var starts = new Vector3[plates.Length];
            for (int i = 0; i < starts.Length; i++) starts[i] = plates[i].position + Vector3.up * .15f;
            float progress = 0f;
            while (progress < 1f)
            {
                progress += Time.deltaTime / .9f;
                if (Time.timeScale > 0f)
                    foreach (Vector3 start in starts)
                    {
                        Vector3 point = Vector3.Lerp(start, destination, progress) + Vector3.up * Mathf.Sin(progress * Mathf.PI) * .35f;
                        Emit(energy, point, Vector3.up * .08f, powered);
                    }
                yield return null;
            }
            for (int i = 0; i < 24; i++) Emit(energy, destination, Random.insideUnitSphere * .7f, powered);
            if (shortcut != null)
            {
                Vector3 start = destination;
                destination = shortcut.position + Vector3.up;
                progress = 0f;
                while (progress < 1f)
                {
                    progress += Time.deltaTime / 1.2f;
                    if (Time.timeScale > 0f) Emit(energy, Vector3.Lerp(start, destination, progress), Vector3.zero, powered);
                    yield return null;
                }
                for (int i = 0; i < 18; i++) Emit(energy, destination, Random.insideUnitSphere * .5f, powered);
            }
        }

        private static void Emit(ParticleSystem system, Vector3 position, Vector3 velocity, Color color)
        {
            system.Emit(new ParticleSystem.EmitParams { position = position, velocity = velocity, startColor = color }, 1);
        }

        private void OnDestroy()
        {
            if (reality != null) reality.DimensionChanged -= WorldChanged;
        }
    }
}
