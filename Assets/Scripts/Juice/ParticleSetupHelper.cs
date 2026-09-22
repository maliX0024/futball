using UnityEngine;

namespace Futball.Juice
{
    /// <summary>
    /// Helper to auto-create star burst & confetti prefabs via menu.
    /// In Editor: Futball > Create Juice Particles
    /// </summary>
    public class ParticleSetupHelper : MonoBehaviour
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Futball/Create Juice Particles")]
        static void CreateParticles()
        {
            CreateStarBurst();
            CreateConfetti();
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log("[Futball] Created StarBurst & Confetti prefabs in Assets/Prefabs/Juice/");
        }

        static void CreateStarBurst()
        {
            var go = new GameObject("StarBurst");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.45f;
            main.startSpeed = 7f;
            main.startSize = 0.22f;
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 28) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.radial = new ParticleSystem.MinMaxCurve(-2f);

            var sizeOver = ps.sizeOverLifetime;
            sizeOver.enabled = true;
            sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0, 1), new Keyframe(0.2f, 1.4f), new Keyframe(1, 0)
            ));

            var colorOver = ps.colorOverLifetime;
            colorOver.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.3f), new GradientAlphaKey(0f, 1f) }
            );
            colorOver.color = g;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));

            SaveAsPrefab(go, "Assets/Prefabs/Juice/StarBurst.prefab");
            DestroyImmediate(go);
        }

        static void CreateConfetti()
        {
            var go = new GameObject("Confetti");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.8f;
            main.startSpeed = 4.5f;
            main.startSize3D = true;
            main.startSizeX = 0.14f; main.startSizeY = 0.22f; main.startSizeZ = 0.14f;
            main.startColor = new Color(1f, 0.85f, 0.2f);
            main.gravityModifier = 0.55f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 65) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(4f, 0.1f, 2f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.y = new ParticleSystem.MinMaxCurve(-1.5f);

            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-180f, 180f);

            var colorOver = ps.colorOverLifetime;
            colorOver.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) }
            );
            colorOver.color = g;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            SaveAsPrefab(go, "Assets/Prefabs/Juice/Confetti.prefab");
            DestroyImmediate(go);
        }

        static void SaveAsPrefab(GameObject go, string path)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            // Use PrefabUtility if available
            #if UNITY_EDITOR
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(go, path);
            #endif
        }
#endif
    }
}
