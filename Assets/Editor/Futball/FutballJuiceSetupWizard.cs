// Futball Juice — One-Click Setup Wizard
// Unity 2021.3+ / 2022+ / 6000+  |  URP / Built-in  |  No external dependencies
// Menu: Futball → Setup Wizard (One Click)
// Installs everything you requested: GOAL stack, Camera Juice, Sound Table, Haptics, Net, Ball FX
// Safe to run multiple times — it fixes missing refs, never duplicates managers.

#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Futball.EditorTools
{
    public class FutballJuiceSetupWizard : EditorWindow
    {
        // ---------- UI STATE ----------
        bool optManagers = true;
        bool optCamera = true;
        bool optFlashCanvas = true;
        bool optParticles = true;
        bool optAudioPlaceholders = true;
        bool optBall = true;
        bool optGoalNet = true;
        bool optPlayerDemo = true;
        bool optTagsLayers = true;
        bool optWireAudioManager = true;

        Color teamColor = new Color(0.15f, 0.6f, 1f, 1f);
        Vector2 scroll;

        // ---------- OPEN ----------
        [MenuItem("Futball/Setup Wizard (One Click) _F9", false, 1)]
        public static void Open()
        {
            var w = GetWindow<FutballJuiceSetupWizard>("Futball Setup");
            w.minSize = new Vector2(520, 640);
            w.Show();
        }

        [MenuItem("Futball/Setup/Install Full Juice System (One Click)", false, 10)]
        public static void QuickInstall()
        {
            if (EditorUtility.DisplayDialog("Futball — Install Juice?",
                "This will set up in the CURRENT SCENE:\n\n" +
                "• _Managers (TimeController, AudioManager, GoalJuiceSequence)\n" +
                "• Main Camera → CameraJuice\n" +
                "• Flash Canvas (team-color screen flash)\n" +
                "• StarBurst + Confetti prefabs\n" +
                "• Ball with BallJuice + Trail\n" +
                "• Goal + Net with NetJuice + GoalTrigger\n" +
                "• Full Sound Table (synthetic placeholders so it works instantly)\n" +
                "• Tags (Ball, Post, Player) + Demo helper\n\n" +
                "Safe to run again — it fixes missing refs, won't duplicate.\n\nContinue?",
                "Install", "Cancel"))
            {
                InstallFullSystem(new Color(0.15f, 0.6f, 1f), genAudio: true, createBall: true, createGoal: true);
            }
        }

        void OnGUI()
        {
            DrawHeader();
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("What to install", EditorStyles.boldLabel);
            optManagers = EditorGUILayout.ToggleLeft(new GUIContent("  _Managers — TimeController / AudioManager / GoalJuiceSequence"), optManagers);
            optCamera = EditorGUILayout.ToggleLeft(new GUIContent("  Main Camera → CameraJuice (Kick Zoom + Trauma Shake + Goal Pan)"), optCamera);
            optFlashCanvas = EditorGUILayout.ToggleLeft(new GUIContent("  Flash Canvas — fullscreen team-color flash (0.18s)"), optFlashCanvas);
            optParticles = EditorGUILayout.ToggleLeft(new GUIContent("  Particles — StarBurst (28) + Confetti (65) prefabs"), optParticles);
            optWireAudioManager = EditorGUILayout.ToggleLeft(new GUIContent("  Wire Sound Table — 14 entries (Kick, Charge, Roll, Post, Net, Crowd...)"), optWireAudioManager);
            optAudioPlaceholders = EditorGUILayout.ToggleLeft(new GUIContent("    └ Generate synthetic placeholder WAVs (hear it instantly)"), optAudioPlaceholders);
            optBall = EditorGUILayout.ToggleLeft(new GUIContent("  Ball — BallJuice + TrailRenderer + Rigidbody"), optBall);
            optGoalNet = EditorGUILayout.ToggleLeft(new GUIContent("  Goal + Net — NetJuice + GoalTrigger (IsTrigger)"), optGoalNet);
            optPlayerDemo = EditorGUILayout.ToggleLeft(new GUIContent("  Player + Demo — KickChargeController + FutballJuiceDemo (G/P/K test)"), optPlayerDemo);
            optTagsLayers = EditorGUILayout.ToggleLeft(new GUIContent("  Project Tags — Add 'Ball', 'Post', 'Player' if missing"), optTagsLayers);

            EditorGUILayout.Space(8);
            teamColor = EditorGUILayout.ColorField(new GUIContent("Team Color (Flash / Confetti)"), teamColor);
            EditorGUILayout.HelpBox("This color tints Screen Flash + StarBurst + Confetti. Change per-goal later via GoalTrigger.teamColor.", MessageType.None);

            EditorGUILayout.Space(10);
            DrawValidationBox();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(6);

            // --- BIG BUTTON ---
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
            if (GUILayout.Button("⚡ INSTALL / REPAIR FULL JUICE SYSTEM", GUILayout.Height(44)))
            {
                InstallWithOptions();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Only", GUILayout.Height(26))) ValidateOnly();
            if (GUILayout.Button("Generate Particles Only", GUILayout.Height(26))) FutballAutoSetup.EnsureParticles(true);
            if (GUILayout.Button("Generate Placeholder Sounds", GUILayout.Height(26))) FutballAutoSetup.GeneratePlaceholderAudioAndWire();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("Test after install: Press Play → G = GOAL full stack • P = Post CLANK • K = Kick • H/J/B = shakes • 1-0 = sounds", MessageType.Info);
            EditorGUILayout.HelpBox("All time effects use unscaledDeltaTime — HitStop, SlowMo, Shake & Zoom keep working while frozen.", MessageType.None);
        }

        void DrawHeader()
        {
            var style = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true };
            GUILayout.Space(6);
            EditorGUILayout.LabelField("<size=16><b>FUTBALL — JUICE SETUP WIZARD</b></size>", style);
            EditorGUILayout.LabelField("<i>Hit Stop → Heavy Shake → Net Stretch → Stars → GOAAAL! + Crowd → Flash → SlowMo → Confetti + Haptic</i>", style);
            EditorGUILayout.LabelField("Unity 2021.3+ • URP/Built-in • No packages required • Safe to re-run", style);
            var rect = GUILayoutUtility.GetLastRect();
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax + 2, rect.width, 1), new Color(0, 0, 0, 0.15f));
            GUILayout.Space(8);
        }

        void DrawValidationBox()
        {
            var issues = FutballAutoSetup.GatherIssues();
            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("✓ All systems wired — you're good to Play!", MessageType.Info);
                return;
            }
            EditorGUILayout.HelpBox(string.Join("\n", issues.Select(s => "• " + s)), MessageType.Warning);
        }

        void ValidateOnly()
        {
            var issues = FutballAutoSetup.GatherIssues();
            if (issues.Count == 0) EditorUtility.DisplayDialog("Futball — Validate", "✓ All systems wired!", "Nice");
            else EditorUtility.DisplayDialog("Futball — Validate", string.Join("\n", issues), "Fix it for me");
        }

        void InstallWithOptions()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Futball Juice", "Installing...", 0.1f);
                Undo.IncrementCurrentGroup();
                int group = Undo.GetCurrentGroup();

                if (optTagsLayers) FutballAutoSetup.EnsureTags();
                if (optManagers) FutballAutoSetup.EnsureManagers(teamColor);
                if (optCamera) FutballAutoSetup.EnsureCameraJuice();
                if (optFlashCanvas) FutballAutoSetup.EnsureFlashCanvas(teamColor);
                if (optParticles) FutballAutoSetup.EnsureParticles(true);
                if (optWireAudioManager) FutballAutoSetup.EnsureAudioLibrary();
                if (optAudioPlaceholders) FutballAutoSetup.GeneratePlaceholderAudioAndWire();
                if (optBall) FutballAutoSetup.EnsureBall(teamColor);
                if (optGoalNet) FutballAutoSetup.EnsureGoalAndNet(teamColor);
                if (optPlayerDemo) FutballAutoSetup.EnsurePlayerAndDemo();

                // Wire cross-references after all objects exist
                FutballAutoSetup.WireAllReferences(teamColor);

                EditorUtility.ClearProgressBar();
                Undo.CollapseUndoOperations(group);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();

                var issues = FutballAutoSetup.GatherIssues();
                string msg = issues.Count == 0
                    ? "✓ Juice installed & wired!\n\nPress Play and hit G for the full GOAL stack."
                    : "Installed with warnings:\n• " + string.Join("\n• ", issues) + "\n\nYou can re-run the wizard to repair.";
                EditorUtility.DisplayDialog("Futball — Done", msg, "Let's Go ⚽");
                Debug.Log("[Futball] Setup complete. " + (issues.Count == 0 ? "All wired." : issues.Count + " warnings — see Wizard."));
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Futball — Error", e.Message + "\n\nSee Console for details.", "OK");
            }
        }

        public static void InstallFullSystem(Color team, bool genAudio, bool createBall, bool createGoal)
        {
            FutballAutoSetup.EnsureTags();
            FutballAutoSetup.EnsureManagers(team);
            FutballAutoSetup.EnsureCameraJuice();
            FutballAutoSetup.EnsureFlashCanvas(team);
            FutballAutoSetup.EnsureParticles(true);
            FutballAutoSetup.EnsureAudioLibrary();
            if (genAudio) FutballAutoSetup.GeneratePlaceholderAudioAndWire();
            if (createBall) FutballAutoSetup.EnsureBall(team);
            if (createGoal) FutballAutoSetup.EnsureGoalAndNet(team);
            FutballAutoSetup.EnsurePlayerAndDemo();
            FutballAutoSetup.WireAllReferences(team);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[Futball] Quick Install complete. Press Play → G = GOAL");
        }
    }

    // ========================================================================
    // AUTO SETUP — does the actual work (callable from Window or Menu)
    // ========================================================================
    public static class FutballAutoSetup
    {
        const string kManagersName = "_Managers";
        const string kFlashCanvasName = "Futball_FlashCanvas";
        const string kBallName = "Ball";
        const string kGoalName = "Goal";
        const string kNetName = "Net";
        const string kPlayerName = "Player";
        const string kParticleStarPath = "Assets/Prefabs/Juice/StarBurst.prefab";
        const string kParticleConfettiPath = "Assets/Prefabs/Juice/Confetti.prefab";
        const string kAudioPlaceholderDir = "Assets/Audio/Placeholders";

        // ---------- TAGS ----------
        public static void EnsureTags()
        {
            AddTag("Ball");
            AddTag("Post");
            AddTag("Player");
        }

        static void AddTag(string tag)
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (asset == null || asset.Length == 0) return;
            var so = new SerializedObject(asset[0]);
            var tags = so.FindProperty("tags");
            bool exists = false;
            for (int i = 0; i < tags.arraySize; i++) if (tags.GetArrayElementAtIndex(i).stringValue == tag) { exists = true; break; }
            if (!exists)
            {
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
                so.ApplyModifiedProperties();
                Debug.Log($"[Futball] Added tag: {tag}");
            }
        }

        // ---------- MANAGERS ----------
        public static GameObject EnsureManagers(Color teamColor)
        {
            var go = GameObject.Find(kManagersName);
            if (go == null)
            {
                go = new GameObject(kManagersName);
                Undo.RegisterCreatedObjectUndo(go, "Create _Managers");
                go.transform.position = Vector3.zero;
            }

            EnsureComponent<Futball.Juice.TimeController>(go);
            var audio = EnsureComponent<Futball.Audio.AudioManager>(go);
            var goal = EnsureComponent<Futball.Juice.GoalJuiceSequence>(go);

            // Defaults for GOAL stack (your spec)
            var tc = go.GetComponent<Futball.Juice.TimeController>();
            tc.goalHitStopDuration = 0.07f;
            tc.slowMoFactor = 0.32f;
            tc.slowMoDuration = 0.55f;

            var gj = go.GetComponent<Futball.Juice.GoalJuiceSequence>();
            gj.hitStopDuration = 0.07f;
            gj.slowMoFactor = 0.32f;
            gj.slowMoDuration = 0.55f;
            gj.flashPeakAlpha = 0.45f;
            gj.flashDuration = 0.18f;

            // Ensure child audio sources exist (AudioManager will create if missing, but we pre-create for visibility)
            EditorUtility.SetDirty(go);
            return go;
        }

        // ---------- CAMERA ----------
        public static Camera EnsureCameraJuice()
        {
            Camera cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindObjectOfType<Camera>();
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
                Undo.RegisterCreatedObjectUndo(camGo, "Create Main Camera");
            }

            var cj = EnsureComponent<Futball.Juice.CameraJuice>(cam.gameObject);
            if (cj.cam == null) cj.cam = cam;
            cj.zoomAmount = 0.10f;
            cj.traumaDecay = 1.6f;
            cj.maxShakeTranslation = 0.35f;
            cj.maxShakeRotation = 1.8f;
            cj.shakeFrequency = 22f;
            cj.goalPanDuration = 0.65f;
            cj.whipDuration = 0.28f;

            // Capture FOV for zoom lerps
            cj.CaptureInitialFOV();
            EditorUtility.SetDirty(cam.gameObject);
            return cam;
        }

        // ---------- FLASH CANVAS ----------
        public static (Canvas canvas, Image image) EnsureFlashCanvas(Color teamColor)
        {
            var existing = GameObject.Find(kFlashCanvasName);
            Canvas canvas;
            Image img;

            if (existing != null)
            {
                canvas = existing.GetComponent<Canvas>();
                img = existing.GetComponentInChildren<Image>();
                if (img == null) img = CreateFlashImage(existing.transform);
            }
            else
            {
                var go = new GameObject(kFlashCanvasName);
                Undo.RegisterCreatedObjectUndo(go, "Create Flash Canvas");
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                go.AddComponent<GraphicRaycaster>();

                var imgGo = new GameObject("Flash");
                imgGo.transform.SetParent(go.transform, false);
                img = imgGo.AddComponent<Image>();
                img.color = new Color(teamColor.r, teamColor.g, teamColor.b, 0f);
                img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            }

            // Ensure it's fullscreen
            if (img != null)
            {
                var rt = img.rectTransform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                var c = img.color; c.a = 0f; img.color = c;
                img.raycastTarget = false;
                EditorUtility.SetDirty(img.gameObject);
            }

            if (canvas != null) EditorUtility.SetDirty(canvas.gameObject);
            return (canvas, img);
        }

        static Image CreateFlashImage(Transform parent)
        {
            var go = new GameObject("Flash");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return img;
        }

        // ---------- PARTICLES ----------
        public static void EnsureParticles(bool overwrite = false)
        {
            Directory.CreateDirectory("Assets/Prefabs/Juice");

            bool starExists = File.Exists(kParticleStarPath);
            bool confExists = File.Exists(kParticleConfettiPath);

            if (!starExists || overwrite) CreateStarBurstPrefab();
            if (!confExists || overwrite) CreateConfettiPrefab();

            AssetDatabase.Refresh();
        }

        static void CreateStarBurstPrefab()
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
            main.duration = 0.6f;
            main.loop = false;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

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
            sizeOver.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.2f, 1.4f), new Keyframe(1, 0)));

            var colorOver = ps.colorOverLifetime;
            colorOver.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.3f), new GradientAlphaKey(0f, 1f) }
            );
            colorOver.color = g;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            renderer.material = mat;

            SavePrefab(go, kParticleStarPath);
            UnityEngine.Object.DestroyImmediate(go);
            Debug.Log("[Futball] Created " + kParticleStarPath);
        }

        static void CreateConfettiPrefab()
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
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 65) });

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
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) }
            );
            colorOver.color = g;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            renderer.material = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            SavePrefab(go, kParticleConfettiPath);
            UnityEngine.Object.DestroyImmediate(go);
            Debug.Log("[Futball] Created " + kParticleConfettiPath);
        }

        static void SavePrefab(GameObject go, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // Use PrefabUtility for proper prefab
            PrefabUtility.SaveAsPrefabAsset(go, path);
        }

        // ---------- AUDIO LIBRARY ----------
        public static void EnsureAudioLibrary()
        {
            var mgrGo = GameObject.Find(kManagersName);
            if (mgrGo == null) mgrGo = EnsureManagers(Color.cyan);
            var audio = mgrGo.GetComponent<Futball.Audio.AudioManager>();
            if (audio == null) audio = EnsureComponent<Futball.Audio.AudioManager>(mgrGo);

            // Use SerializedObject to populate list so undo works
            var so = new SerializedObject(audio);
            var sounds = so.FindProperty("sounds");
            // If already populated with 10+ entries, don't overwrite clips but ensure types exist
            var required = new[]
            {
                Futball.Audio.SoundType.KickSoft, Futball.Audio.SoundType.KickMedium, Futball.Audio.SoundType.KickHard,
                Futball.Audio.SoundType.Charge, Futball.Audio.SoundType.Roll, Futball.Audio.SoundType.Bounce,
                Futball.Audio.SoundType.PostHit, Futball.Audio.SoundType.NetSoft, Futball.Audio.SoundType.NetHard,
                Futball.Audio.SoundType.CrowdIdle, Futball.Audio.SoundType.CrowdGasp, Futball.Audio.SoundType.CrowdCheer,
                Futball.Audio.SoundType.GoalShout, Futball.Audio.SoundType.Whistle, Futball.Audio.SoundType.UIPop, Futball.Audio.SoundType.Coin
            };

            // Build lookup of existing types
            var existingTypes = new HashSet<Futball.Audio.SoundType>();
            for (int i = 0; i < sounds.arraySize; i++)
            {
                var el = sounds.GetArrayElementAtIndex(i);
                var type = (Futball.Audio.SoundType)el.FindPropertyRelative("type").enumValueIndex;
                existingTypes.Add(type);
            }

            foreach (var req in required)
            {
                if (existingTypes.Contains(req)) continue;
                sounds.InsertArrayElementAtIndex(sounds.arraySize);
                var el = sounds.GetArrayElementAtIndex(sounds.arraySize - 1);
                el.FindPropertyRelative("type").enumValueIndex = (int)req;
                el.FindPropertyRelative("volume").floatValue = DefaultVolume(req);
                el.FindPropertyRelative("basePitch").floatValue = 1f;
                el.FindPropertyRelative("loop").boolValue = (req == Futball.Audio.SoundType.CrowdIdle || req == Futball.Audio.SoundType.Roll || req == Futball.Audio.SoundType.Charge);
                // clips array stays empty until placeholders or manual assign
            }

            so.ApplyModifiedProperties();
            EnsureAudioSources(mgrGo, audio);
            EditorUtility.SetDirty(audio);
            Debug.Log("[Futball] Audio library wired (" + sounds.arraySize + " entries). Generate placeholders or assign your own WAVs.");
        }

        static float DefaultVolume(Futball.Audio.SoundType t)
        {
            switch (t)
            {
                case Futball.Audio.SoundType.CrowdIdle: return 0.55f;
                case Futball.Audio.SoundType.Roll: return 0.45f;
                case Futball.Audio.SoundType.Charge: return 0.55f;
                default: return 1f;
            }
        }

        static void EnsureAudioSources(GameObject mgrGo, Futball.Audio.AudioManager audio)
        {
            // Ensure 4 sources via SerializedObject
            var so = new SerializedObject(audio);
            EnsureSourceProperty(so, "sfxSource", mgrGo, "_SFX", false);
            EnsureSourceProperty(so, "crowdIdleSource", mgrGo, "CrowdIdle", true);
            EnsureSourceProperty(so, "crowdOneShotSource", mgrGo, "CrowdOneShot", false);
            EnsureSourceProperty(so, "rollSource", mgrGo, "RollLoop", true);
            EnsureSourceProperty(so, "chargeSource", mgrGo, "ChargeLoop", true);
            so.ApplyModifiedProperties();
        }

        static void EnsureSourceProperty(SerializedObject so, string propName, GameObject parent, string childName, bool loop)
        {
            var prop = so.FindProperty(propName);
            if (prop.objectReferenceValue != null) return;
            var child = new GameObject(childName);
            child.transform.SetParent(parent.transform);
            var src = child.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            Undo.RegisterCreatedObjectUndo(child, "Create " + childName);
            prop.objectReferenceValue = src;
        }

        // ---------- PLACEHOLDER AUDIO (synthetic WAVs) ----------
        public static void GeneratePlaceholderAudioAndWire()
        {
            Directory.CreateDirectory(kAudioPlaceholderDir);
            var generated = new Dictionary<Futball.Audio.SoundType, string>();

            // Generate synthetic tones — all 16-bit mono 44100
            generated[Futball.Audio.SoundType.KickSoft]  = WavGenerator.Save(kAudioPlaceholderDir + "/kick_soft.wav", WavGenerator.KickSoft());
            generated[Futball.Audio.SoundType.KickMedium] = WavGenerator.Save(kAudioPlaceholderDir + "/kick_medium.wav", WavGenerator.KickMedium());
            generated[Futball.Audio.SoundType.KickHard]  = WavGenerator.Save(kAudioPlaceholderDir + "/kick_hard.wav", WavGenerator.KickHard());
            generated[Futball.Audio.SoundType.Charge]    = WavGenerator.Save(kAudioPlaceholderDir + "/charge.wav", WavGenerator.Charge());
            generated[Futball.Audio.SoundType.Roll]      = WavGenerator.Save(kAudioPlaceholderDir + "/roll.wav", WavGenerator.Roll());
            generated[Futball.Audio.SoundType.Bounce]    = WavGenerator.Save(kAudioPlaceholderDir + "/bounce.wav", WavGenerator.Bounce());
            generated[Futball.Audio.SoundType.PostHit]   = WavGenerator.Save(kAudioPlaceholderDir + "/post_clank.wav", WavGenerator.PostClank());
            generated[Futball.Audio.SoundType.NetSoft]   = WavGenerator.Save(kAudioPlaceholderDir + "/net_soft.wav", WavGenerator.NetSoft());
            generated[Futball.Audio.SoundType.NetHard]   = WavGenerator.Save(kAudioPlaceholderDir + "/net_hard.wav", WavGenerator.NetHard());
            generated[Futball.Audio.SoundType.CrowdIdle] = WavGenerator.Save(kAudioPlaceholderDir + "/crowd_idle.wav", WavGenerator.CrowdIdle());
            generated[Futball.Audio.SoundType.CrowdGasp] = WavGenerator.Save(kAudioPlaceholderDir + "/crowd_gasp.wav", WavGenerator.CrowdGasp());
            generated[Futball.Audio.SoundType.CrowdCheer]= WavGenerator.Save(kAudioPlaceholderDir + "/crowd_cheer.wav", WavGenerator.CrowdCheer());
            generated[Futball.Audio.SoundType.GoalShout] = WavGenerator.Save(kAudioPlaceholderDir + "/goaaal.wav", WavGenerator.GoalShout());
            generated[Futball.Audio.SoundType.Whistle]   = WavGenerator.Save(kAudioPlaceholderDir + "/whistle.wav", WavGenerator.Whistle());
            generated[Futball.Audio.SoundType.UIPop]     = WavGenerator.Save(kAudioPlaceholderDir + "/ui_pop.wav", WavGenerator.UIPop());
            generated[Futball.Audio.SoundType.Coin]      = WavGenerator.Save(kAudioPlaceholderDir + "/coin.wav", WavGenerator.Coin());

            AssetDatabase.Refresh();
            // Import settings: force 2D, no 3D, load in background off
            foreach (var kv in generated) SetAudioImportSettings(kv.Value, kv.Key);

            AssetDatabase.Refresh();
            WireClipsToManager(generated);
            Debug.Log("[Futball] Placeholder WAVs generated in " + kAudioPlaceholderDir + " — replace with Mixkit/Freesound when ready.");
        }

        static void SetAudioImportSettings(string path, Futball.Audio.SoundType type)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.preloadAudioData = (type != Futball.Audio.SoundType.CrowdIdle && type != Futball.Audio.SoundType.Roll && type != Futball.Audio.SoundType.Charge);
            importer.loadInBackground = false;
            importer.SaveAndReimport();
        }

        static void WireClipsToManager(Dictionary<Futball.Audio.SoundType, string> map)
        {
            var mgrGo = GameObject.Find(kManagersName);
            if (mgrGo == null) return;
            var audio = mgrGo.GetComponent<Futball.Audio.AudioManager>();
            if (audio == null) return;

            var so = new SerializedObject(audio);
            var sounds = so.FindProperty("sounds");
            for (int i = 0; i < sounds.arraySize; i++)
            {
                var el = sounds.GetArrayElementAtIndex(i);
                var type = (Futball.Audio.SoundType)el.FindPropertyRelative("type").enumValueIndex;
                if (map.TryGetValue(type, out var path))
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip != null)
                    {
                        var clipsArr = el.FindPropertyRelative("clips");
                        // Only wire if empty or contains null (don't overwrite user clips)
                        bool isEmpty = clipsArr.arraySize == 0 || clipsArr.GetArrayElementAtIndex(0).objectReferenceValue == null;
                        if (isEmpty)
                        {
                            clipsArr.arraySize = 1;
                            clipsArr.GetArrayElementAtIndex(0).objectReferenceValue = clip;
                        }
                    }
                }
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(audio);
        }

        // ---------- BALL ----------
        public static GameObject EnsureBall(Color teamColor)
        {
            var ball = GameObject.Find(kBallName);
            if (ball == null)
            {
                // Search by BallJuice
                var existing = UnityEngine.Object.FindObjectOfType<Futball.Juice.BallJuice>();
                if (existing != null) ball = existing.gameObject;
            }

            if (ball == null)
            {
                ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name = kBallName;
                ball.tag = "Ball";
                Undo.RegisterCreatedObjectUndo(ball, "Create Ball");
                ball.transform.position = new Vector3(0, 0.5f, 3f);
                ball.transform.localScale = Vector3.one * 0.45f;
                // Fix collider + add RB
                var rb = ball.AddComponent<Rigidbody>();
                rb.mass = 0.45f; rb.drag = 0.05f; rb.angularDrag = 0.05f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                // Material
                var rend = ball.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Standard"));
                    mat.color = Color.white;
                    rend.material = mat;
                }
                // Ground plane helper? Create shadow decal child
                var shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shadow.name = "ShadowBlob";
                shadow.transform.SetParent(ball.transform);
                shadow.transform.localPosition = new Vector3(0, -0.4f, 0);
                shadow.transform.localScale = new Vector3(1.2f, 0.02f, 1.2f);
                var sr = shadow.GetComponent<Renderer>();
                if (sr != null) { var m = new Material(Shader.Find("Standard")); m.color = new Color(0,0,0,0.22f); sr.material = m; }
                DestroyImmediate(shadow.GetComponent<Collider>());

                // Trail
                var trail = ball.AddComponent<TrailRenderer>();
                trail.time = 0.18f; trail.minVertexDistance = 0.06f;
                trail.startWidth = 0.12f; trail.endWidth = 0.01f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
                trail.startColor = new Color(teamColor.r, teamColor.g, teamColor.b, 0.85f);
                trail.endColor = new Color(teamColor.r, teamColor.g, teamColor.b, 0f);
                trail.emitting = false;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var bj = EnsureComponent<Futball.Juice.BallJuice>(ball);
            // Try to find visual child (mesh)
            if (bj.visual == null)
            {
                // If ball is primitive, visual is self
                bj.visual = ball.transform;
            }
            if (bj.trail == null) bj.trail = ball.GetComponent<TrailRenderer>();
            bj.kickSquashAmount = 0.32f;
            bj.bounceSquashAmount = 0.20f;
            bj.squashDuration = 0.18f;
            bj.flightStretchFactor = 0.18f;

            ball.tag = "Ball";
            EditorUtility.SetDirty(ball);
            return ball;
        }

        // ---------- GOAL + NET ----------
        public static (GameObject goal, GameObject net) EnsureGoalAndNet(Color teamColor)
        {
            // Goal trigger
            var goal = GameObject.Find(kGoalName);
            if (goal == null)
            {
                var existing = UnityEngine.Object.FindObjectOfType<Futball.Juice.GoalTrigger>();
                if (existing != null) goal = existing.gameObject;
            }
            if (goal == null)
            {
                goal = new GameObject(kGoalName);
                Undo.RegisterCreatedObjectUndo(goal, "Create Goal");
                goal.transform.position = new Vector3(0, 1f, 10f);
                var col = goal.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(7.32f, 2.44f, 1.2f); // real goal size
                // Visual frame
                var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frame.name = "Frame";
                frame.transform.SetParent(goal.transform);
                frame.transform.localPosition = Vector3.zero;
                frame.transform.localScale = new Vector3(7.5f, 2.6f, 0.2f);
                var fr = frame.GetComponent<Renderer>();
                if (fr != null) { var m = new Material(Shader.Find("Standard")); m.color = Color.white; fr.material = m; }
                DestroyImmediate(frame.GetComponent<Collider>());
            }

            var trigger = EnsureComponent<Futball.Juice.GoalTrigger>(goal);
            trigger.teamColor = teamColor;
            trigger.ballTag = "Ball";
            // net reference wired later

            // Net
            var net = GameObject.Find(kNetName);
            if (net == null) net = GameObject.Find("Net");
            if (net == null)
            {
                // Look for child of goal named Net
                var child = goal.transform.Find("Net");
                if (child != null) net = child.gameObject;
            }
            if (net == null)
            {
                net = GameObject.CreatePrimitive(PrimitiveType.Cube);
                net.name = kNetName;
                Undo.RegisterCreatedObjectUndo(net, "Create Net");
                net.transform.SetParent(goal.transform);
                net.transform.localPosition = new Vector3(0, 0, 0.35f);
                net.transform.localScale = new Vector3(7f, 2.2f, 0.15f);
                var nr = net.GetComponent<Renderer>();
                if (nr != null)
                {
                    var m = new Material(Shader.Find("Standard"));
                    m.color = new Color(0.92f, 0.92f, 0.92f, 0.85f);
                    // Try transparent
                    m.SetFloat("_Mode", 3);
                    m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    m.SetInt("_ZWrite", 0);
                    m.DisableKeyword("_ALPHATEST_ON");
                    m.EnableKeyword("_ALPHABLEND_ON");
                    m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    m.renderQueue = 3000;
                    nr.material = m;
                }
                DestroyImmediate(net.GetComponent<Collider>());
                // Add mesh filter for ripple if needed — cube already has one
            }

            var netJuice = EnsureComponent<Futball.Juice.NetJuice>(net);
            netJuice.stretchScale = new Vector3(1.25f, 0.85f, 1.25f);
            netJuice.stretchDuration = 0.45f;
            netJuice.rippleAmplitude = 0.18f;
            // Auto-assign meshFilter if net has one
            if (netJuice.meshFilter == null) netJuice.meshFilter = net.GetComponent<MeshFilter>();

            // Wire trigger to net
            trigger.net = netJuice;

            EditorUtility.SetDirty(goal);
            EditorUtility.SetDirty(net);
            return (goal, net);
        }

        // ---------- PLAYER + DEMO ----------
        public static void EnsurePlayerAndDemo()
        {
            var ball = GameObject.Find(kBallName) ?? UnityEngine.Object.FindObjectOfType<Futball.Juice.BallJuice>()?.gameObject;

            // Player (simple capsule)
            var player = GameObject.Find(kPlayerName);
            if (player == null) player = GameObject.Find("PlayerCapsule");
            if (player == null)
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = kPlayerName;
                player.tag = "Player";
                Undo.RegisterCreatedObjectUndo(player, "Create Player");
                player.transform.position = new Vector3(0, 1f, 0f);
                // Kick point at foot
                var kickPoint = new GameObject("KickPoint");
                kickPoint.transform.SetParent(player.transform);
                kickPoint.transform.localPosition = new Vector3(0, -0.7f, 0.6f);
            }
            else player.tag = "Player";

            // Ensure KickController
            var kcc = EnsureComponent<Futball.Player.KickChargeController>(player);
            if (kcc.kickPoint == null)
            {
                var kp = player.transform.Find("KickPoint");
                if (kp != null) kcc.kickPoint = kp;
                else
                {
                    var kp2 = new GameObject("KickPoint");
                    kp2.transform.SetParent(player.transform);
                    kp2.transform.localPosition = new Vector3(0, -0.7f, 0.6f);
                    kcc.kickPoint = kp2.transform;
                }
            }
            if (ball != null)
            {
                kcc.ballRb = ball.GetComponent<Rigidbody>();
                kcc.ballJuice = ball.GetComponent<Futball.Juice.BallJuice>();
            }
            kcc.maxChargeTime = 1.1f;
            kcc.minKickForce = 6f;
            kcc.maxKickForce = 18f;

            // Demo helper on _Managers or separate
            var managers = GameObject.Find(kManagersName);
            GameObject demoHost = managers != null ? managers : player;
            var demo = EnsureComponent<Futball.Demo.FutballJuiceDemo>(demoHost);
            demo.ball = ball != null ? ball.transform : null;
            demo.scorer = player.transform;
            demo.teamColor = new Color(0.15f, 0.6f, 1f);

            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(demoHost);
        }

        // ---------- WIRING ----------
        public static void WireAllReferences(Color teamColor)
        {
            var managers = GameObject.Find(kManagersName);
            if (managers == null) return;

            var goalSeq = managers.GetComponent<Futball.Juice.GoalJuiceSequence>();
            var flashImg = GameObject.Find(kFlashCanvasName)?.GetComponentInChildren<Image>();
            var ball = GameObject.Find(kBallName) ?? UnityEngine.Object.FindObjectOfType<Futball.Juice.BallJuice>()?.gameObject;
            var net = GameObject.Find(kNetName) ?? GameObject.Find("Net");
            var netJuice = net != null ? net.GetComponent<Futball.Juice.NetJuice>() : null;
            var ballT = ball != null ? ball.transform : null;

            if (goalSeq != null)
            {
                if (flashImg != null) goalSeq.screenFlashImage = flashImg;
                if (netJuice != null) goalSeq.netJuice = netJuice;
                if (ballT != null) goalSeq.ballTransform = ballT;

                // Load prefabs if exist
                var star = AssetDatabase.LoadAssetAtPath<ParticleSystem>(kParticleStarPath);
                var conf = AssetDatabase.LoadAssetAtPath<ParticleSystem>(kParticleConfettiPath);
                if (star != null) goalSeq.starBurstPrefab = star;
                if (conf != null) goalSeq.confettiPrefab = conf;

                EditorUtility.SetDirty(goalSeq);
            }

            var ballJuice = ball != null ? ball.GetComponent<Futball.Juice.BallJuice>() : null;
            if (ballJuice != null)
            {
                var star = AssetDatabase.LoadAssetAtPath<ParticleSystem>(kParticleStarPath);
                if (star != null && ballJuice.kickBurstPrefab == null) ballJuice.kickBurstPrefab = star;
                // post spark -> reuse star but smaller (we duplicate scale in code, so same prefab ok)
                if (star != null && ballJuice.postSparkPrefab == null) ballJuice.postSparkPrefab = star;
                EditorUtility.SetDirty(ballJuice);
            }

            var trigger = UnityEngine.Object.FindObjectOfType<Futball.Juice.GoalTrigger>();
            if (trigger != null)
            {
                trigger.teamColor = teamColor;
                if (ballT != null) trigger.ballTransform = ballT;
                if (netJuice != null) trigger.net = netJuice;
                EditorUtility.SetDirty(trigger);
            }

            // Ensure camera captured FOV after wiring
            var camJuice = UnityEngine.Object.FindObjectOfType<Futball.Juice.CameraJuice>();
            if (camJuice != null) camJuice.CaptureInitialFOV();
        }

        // ---------- VALIDATION ----------
        public static List<string> GatherIssues()
        {
            var list = new List<string>();
            if (GameObject.Find(kManagersName) == null) list.Add("_Managers missing — run Setup Wizard");
            else
            {
                var m = GameObject.Find(kManagersName);
                if (m.GetComponent<Futball.Juice.TimeController>() == null) list.Add("TimeController missing on _Managers");
                if (m.GetComponent<Futball.Audio.AudioManager>() == null) list.Add("AudioManager missing on _Managers");
                if (m.GetComponent<Futball.Juice.GoalJuiceSequence>() == null) list.Add("GoalJuiceSequence missing on _Managers");
                else
                {
                    var g = m.GetComponent<Futball.Juice.GoalJuiceSequence>();
                    if (g.screenFlashImage == null) list.Add("GoalJuiceSequence.screenFlashImage not wired (Flash Canvas)");
                    if (g.starBurstPrefab == null) list.Add("StarBurst prefab not assigned — run 'Create Juice Particles'");
                    if (g.confettiPrefab == null) list.Add("Confetti prefab not assigned");
                }
            }

            var cam = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
            if (cam == null) list.Add("No Main Camera in scene");
            else if (cam.GetComponent<Futball.Juice.CameraJuice>() == null) list.Add("CameraJuice missing on Main Camera");

            if (GameObject.Find(kFlashCanvasName) == null) list.Add("Flash Canvas missing (Futball_FlashCanvas)");
            if (!File.Exists(kParticleStarPath)) list.Add("StarBurst prefab missing at " + kParticleStarPath);
            if (!File.Exists(kParticleConfettiPath)) list.Add("Confetti prefab missing at " + kParticleConfettiPath);

            var audioMgr = UnityEngine.Object.FindObjectOfType<Futball.Audio.AudioManager>();
            if (audioMgr != null)
            {
                var so = new SerializedObject(audioMgr);
                var sounds = so.FindProperty("sounds");
                if (sounds.arraySize < 10) list.Add("AudioManager Sound Library incomplete (" + sounds.arraySize + "/16)");
                // Check clips
                int missingClips = 0;
                for (int i = 0; i < sounds.arraySize; i++)
                {
                    var el = sounds.GetArrayElementAtIndex(i);
                    var arr = el.FindPropertyRelative("clips");
                    if (arr.arraySize == 0 || arr.GetArrayElementAtIndex(0).objectReferenceValue == null) missingClips++;
                }
                if (missingClips > 0) list.Add(missingClips + " sounds have no clip — generate placeholders or assign WAVs (wizard can do it)");
            }

            var ball = GameObject.Find(kBallName) ?? (UnityEngine.Object.FindObjectOfType<Futball.Juice.BallJuice>()?.gameObject);
            if (ball == null) list.Add("Ball missing (tag Ball) — wizard will create one");
            else if (ball.GetComponent<Futball.Juice.BallJuice>() == null) list.Add("BallJuice missing on Ball");

            var goal = UnityEngine.Object.FindObjectOfType<Futball.Juice.GoalTrigger>();
            if (goal == null) list.Add("GoalTrigger missing — wizard will create Goal");
            else if (!goal.GetComponent<Collider>().isTrigger) list.Add("Goal collider is not IsTrigger");

            return list;
        }

        // ---------- HELPERS ----------
        static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null)
            {
                c = Undo.AddComponent<T>(go);
            }
            return c;
        }
    }

    // ========================================================================
    // WAV GENERATOR — synthetic placeholders so juice is audible immediately
    // Writes 16-bit PCM mono WAV files to Assets/Audio/Placeholders/
    // ========================================================================
    public static class WavGenerator
    {
        const int SampleRate = 44100;

        public static string Save(string path, float[] samples)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            WriteWav(path, samples, SampleRate);
            return path;
        }

        // ---- Presets for your sound table ----
        public static float[] KickSoft()   => Sine decaying(120f, 0.14f, 28f, 0.55f);
        public static float[] KickMedium() => Sine decaying(190f, 0.13f, 32f, 0.70f, addClick: true);
        public static float[] KickHard()   => KickHardWave();
        public static float[] Charge()     => RisingWhine();
        public static float[] Roll()       => RollRumble();
        public static float[] Bounce()     => Sine decaying(220f, 0.09f, 38f, 0.45f);
        public static float[] PostClank()  => MetalClank();
        public static float[] NetSoft()    => FabricSwish(0.18f, 0.25f);
        public static float[] NetHard()    => FabricSwish(0.22f, 0.45f);
        public static float[] CrowdIdle()  => CrowdLoop(1.8f, 0.035f);
        public static float[] CrowdGasp()  => Gasp();
        public static float[] CrowdCheer() => Cheer();
        public static float[] GoalShout()  => Goaaal();
        public static float[] Whistle()    => WhistleTone();
        public static float[] UIPop()      => Pop();
        public static float[] Coin()       => CoinDing();

        // ---- Generators ----
        static float[] Sine decaying(float freq, float dur, float decay, float vol, bool addClick = false)
        {
            int len = (int)(SampleRate * dur);
            var s = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-decay * t);
                float v = Mathf.Sin(2 * Mathf.PI * freq * t) * env * vol;
                if (addClick && i < 30) v += (UnityEngine.Random.value - 0.5f) * 0.25f * (1f - i / 30f);
                s[i] = Mathf.Clamp(v, -1f, 1f);
            }
            return ApplyFade(s, 0.002f, 0.01f);
        }

        static float[] KickHardWave()
        {
            int len = (int)(SampleRate * 0.26f);
            var s = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                float boom = Mathf.Sin(2 * Mathf.PI * 72f * t) * Mathf.Exp(-18f * t) * 0.9f;
                float punch = Mathf.Sin(2 * Mathf.PI * 160f * t) * Mathf.Exp(-45f * t) * 0.6f;
                float subDrop = Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(120f, 40f, Mathf.Clamp01(t * 8f)) * t) * Mathf.Exp(-12f * t) * 0.35f;
                float click = (i < 12) ? (UnityEngine.Random.value - 0.5f) * 0.5f : 0f;
                s[i] = Mathf.Clamp(boom + punch + subDrop + click, -1f, 1f);
            }
            return ApplyFade(s, 0.001f, 0.02f);
        }

        static float[] RisingWhine()
        {
            float dur = 1.2f;
            int len = (int)(SampleRate * dur);
            var s = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                float prog = t / dur;
                float freq = Mathf.Lerp(220f, 880f, prog * prog);
                float v = Mathf.Sin(2 * Mathf.PI * freq * t) * 0.18f;
                // subtle vibrato
                v *= 1f + 0.08f * Mathf.Sin(2 * Mathf.PI * 18f * t);
                s[i] = v;
            }
            return ApplyFade(s, 0.04f, 0.08f);
        }

        static float[] RollRumble()
        {
            float dur = 0.9f;
            int len = (int)(SampleRate * dur);
            var s = new float[len];
            System.Random rng = new System.Random(42);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                float low = Mathf.Sin(2 * Mathf.PI * 45f * t) * 0.12f;
                float rumble = ((float)rng.NextDouble() * 2f - 1f) * 0.06f;
                // Bandpass-ish: smooth rumble
                s[i] = (low + rumble) * (0.85f + 0.15f * Mathf.Sin(2 * Mathf.PI * 6f * t));
            }
            return ApplyFade(s, 0.05f, 0.05f);
        }

        static float[] MetalClank()
        {
            float dur = 0.65f;
            int len = (int)(SampleRate * dur);
            var s = new float[len];
            float[] freqs = { 980f, 1450f, 2100f, 3100f };
            float[] amps = { 0.5f, 0.32f, 0.22f, 0.12f };
            float[] decays = { 9f, 14f, 22f, 30f };
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                float v = 0f;
                for (int k = 0; k < freqs.Length; k++) v += Mathf.Sin(2 * Mathf.PI * freqs[k] * t) * amps[k] * Mathf.Exp(-decays[k] * t);
                if (i < 4) v += (UnityEngine.Random.value - 0.5f) * 0.7f;
                s[i] = Mathf.Clamp(v * 0.65f, -1f, 1f);
            }
            return ApplyFade(s, 0f, 0.08f);
        }

        static float[] FabricSwish(float dur, float vol)
        {
            int len = (int)(SampleRate * dur);
            var s = new float[len];
            System.Random rng = new System.Random(123);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                float prog = t / dur;
                float env = Mathf.Sin(Mathf.PI * prog) * Mathf.Exp(-6f * prog); // swell then fade
                float noise = ((float)rng.NextDouble() * 2f - 1f);
                // Low-pass noise + whoosh
                float whoosh = Mathf.Sin(2 * Mathf.PI * 180f * t) * 0.08f * env;
                s[i] = Mathf.Clamp((noise * 0.12f + whoosh) * env * vol * 3.5f, -1f, 1f);
            }
            return ApplyFade(s, 0.008f, 0.02f);
        }

        static float[] CrowdLoop(float dur, float vol)
        {
            int len = (int)(SampleRate * dur);
            var s = new float[len];
            System.Random rng = new System.Random(777);
            for (int i = 0; i < len; i++)
            {
                // Brown-ish noise + subtle chant
                float n = ((float)rng.NextDouble() * 2f - 1f);
                // Integrate for brown
                // cheap: use two randoms
                n = (n + ((float)rng.NextDouble() * 2f - 1f)) * 0.5f;
                float chant = Mathf.Sin(2 * Mathf.PI * 110f * i / (float)SampleRate) * 0.02f * Mathf.Sin(2 * Mathf.PI * 0.7f * i / (float)SampleRate + 0.3f);
                s[i] = Mathf.Clamp(n * vol + chant, -1f, 1f);
            }
            // Make loopable: crossfade ends
            int fade = (int)(SampleRate * 0.12f);
            for (int i = 0; i < fade; i++)
            {
                float a = i / (float)fade;
                s[i] = Mathf.Lerp(s[len - fade + i], s[i], a);
            }
            return s;
        }

        static float[] Gasp()
        {
            int len = (int)(SampleRate * 0.32f);
            var s = new float[len];
            System.Random rng = new System.Random(99);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-9f * t) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.18f));
                float n = ((float)rng.NextDouble() * 2f - 1f) * env * 0.35f;
                s[i] = n;
            }
            return ApplyFade(s, 0.005f, 0.02f);
        }

        static float[] Cheer()
        {
            float dur = 1.1f;
            int len = (int)(SampleRate * dur);
            var s = new float[len];
            System.Random rng = new System.Random(555);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                float prog = t / dur;
                float attack = Mathf.Clamp01(t / 0.12f);
                float decay = Mathf.Exp(-1.2f * prog);
                float n = ((float)rng.NextDouble() * 2f - 1f) * 0.22f;
                // Formant-ish vowel "aah"
                float vowel = (Mathf.Sin(2 * Mathf.PI * 280f * t) * 0.07f + Mathf.Sin(2 * Mathf.PI * 560f * t) * 0.035f) * attack;
                s[i] = Mathf.Clamp((n * 0.7f + vowel) * attack * decay, -1f, 1f);
            }
            return ApplyFade(s, 0.01f, 0.12f);
        }

        static float[] Goaaal()
        {
            float dur = 0.85f;
            int len = (int)(SampleRate * dur);
            var s = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                float prog = t / dur;
                float freq = Mathf.Lerp(220f, 330f, Mathf.Clamp01(prog * 1.6f));
                // Pitch glide + vibrato after 0.3s
                float vib = (t > 0.28f) ? 0.04f * Mathf.Sin(2 * Mathf.PI * 6.2f * t) : 0f;
                float v = Mathf.Sin(2 * Mathf.PI * freq * (1f + vib) * t);
                // Harmonics for "shout"
                v += 0.35f * Mathf.Sin(2 * Mathf.PI * freq * 2f * t) * Mathf.Exp(-6f * Mathf.Clamp01(prog * 2f));
                float env = Mathf.Clamp01(t / 0.06f) * Mathf.Exp(-1.8f * prog) * (0.9f + 0.1f * Mathf.Sin(2 * Mathf.PI * 2f * t));
                s[i] = Mathf.Clamp(v * env * 0.42f, -1f, 1f);
            }
            return ApplyFade(s, 0.005f, 0.06f);
        }

        static float[] WhistleTone()
        {
            float dur = 0.38f;
            int len = (int)(SampleRate * dur);
            var s = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                float freq = 1850f + 120f * Mathf.Sin(2 * Mathf.PI * 22f * t);
                float v = Mathf.Sin(2 * Mathf.PI * freq * t) * 0.32f;
                v += Mathf.Sin(2 * Mathf.PI * freq * 2f * t) * 0.08f;
                float env = Mathf.Clamp01(t / 0.02f) * Mathf.Clamp01((dur - t) / 0.07f);
                s[i] = v * env;
            }
            return ApplyFade(s, 0.002f, 0.015f);
        }

        static float[] Pop()
        {
            float dur = 0.12f;
            int len = (int)(SampleRate * dur);
            var s = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                float freq = 650f * Mathf.Exp(-28f * t) + 120f;
                float v = Mathf.Sin(2 * Mathf.PI * freq * t) * Mathf.Exp(-32f * t);
                s[i] = Mathf.Clamp(v * 0.75f, -1f, 1f);
            }
            return s;
        }

        static float[] CoinDing()
        {
            float dur = 0.42f;
            int len = (int)(SampleRate * dur);
            var s = new float[len];
            float f0 = 880f; // A5
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                float v = Mathf.Sin(2 * Mathf.PI * f0 * t) * Mathf.Exp(-5.2f * t) * 0.5f;
                v += Mathf.Sin(2 * Mathf.PI * f0 * 2f * t) * Mathf.Exp(-7f * t) * 0.22f;
                v += Mathf.Sin(2 * Mathf.PI * f0 * 3f * t) * Mathf.Exp(-9f * t) * 0.10f;
                s[i] = Mathf.Clamp(v, -1f, 1f);
            }
            return ApplyFade(s, 0f, 0.02f);
        }

        static float[] ApplyFade(float[] s, float fadeIn, float fadeOut)
        {
            int fi = (int)(SampleRate * fadeIn);
            int fo = (int)(SampleRate * fadeOut);
            for (int i = 0; i < fi && i < s.Length; i++) s[i] *= (i / (float)fi);
            for (int i = 0; i < fo && i < s.Length; i++) s[s.Length - 1 - i] *= (i / (float)fo);
            return s;
        }

        // ---- WAV Writer (16-bit PCM mono) ----
        static void WriteWav(string path, float[] samples, int rate)
        {
            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                int channels = 1;
                int bits = 16;
                int byteRate = rate * channels * bits / 8;
                int blockAlign = channels * bits / 8;
                int dataBytes = samples.Length * channels * bits / 8;

                // RIFF
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + dataBytes);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                // fmt
                w.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                w.Write(16);
                w.Write((short)1); // PCM
                w.Write((short)channels);
                w.Write(rate);
                w.Write(byteRate);
                w.Write((short)blockAlign);
                w.Write((short)bits);
                // data
                w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                w.Write(dataBytes);
                foreach (var f in samples)
                {
                    short v = (short)Mathf.Clamp(f * 32767f, -32768f, 32767f);
                    w.Write(v);
                }
            }
        }
    }
}
#endif
