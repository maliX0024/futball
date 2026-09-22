#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Futball.EditorTools;

namespace Futball.EditorTools
{
    public static class FutballMenuQuickActions
    {
        [MenuItem("Futball/Help/Setup Guide", false, 100)]
        static void OpenGuide()
        {
            var path = "Assets/Documentation/SETUP_GUIDE.md";
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj != null) AssetDatabase.OpenAsset(obj);
            else EditorUtility.DisplayDialog("Futball", "Guide not found at " + path, "OK");
        }

        [MenuItem("Futball/Help/Validate Scene", false, 101)]
        static void Validate()
        {
            var issues = FutballAutoSetup.GatherIssues();
            if (issues.Count == 0) EditorUtility.DisplayDialog("Futball — Validate", "✓ All systems wired!", "OK");
            else EditorUtility.DisplayDialog("Futball — Validate (" + issues.Count + ")", string.Join("\n", issues), "OK");
        }

        [MenuItem("Futball/Setup/Reset Camera FOV", false, 20)]
        static void ResetCam()
        {
            var cj = Object.FindObjectOfType<Futball.Juice.CameraJuice>();
            if (cj != null) { cj.CaptureInitialFOV(); Debug.Log("[Futball] Camera FOV recaptured."); }
            else Debug.LogWarning("[Futball] No CameraJuice found.");
        }

        [MenuItem("Futball/Setup/Generate Particles Only", false, 21)]
        static void GenParticles() => FutballAutoSetup.EnsureParticles(true);

        [MenuItem("Futball/Setup/Generate Placeholder Audio Only", false, 22)]
        static void GenAudio() => FutballAutoSetup.GeneratePlaceholderAudioAndWire();

        [MenuItem("Futball/Test/Play GOAL (Edit Mode)", false, 30)]
        static void TestGoalEdit()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Futball Test", "Enter Play Mode, then press G (or use the Demo buttons).\n\nIn Play Mode: G = GOAL, P = POST, K = Kick", "Got it");
                return;
            }
            var seq = Object.FindObjectOfType<Futball.Juice.GoalJuiceSequence>();
            if (seq != null) seq.PlayGoal(seq.transform.position + Vector3.forward * 3f, new Color(0.15f, 0.6f, 1f), null);
        }
    }
}
#endif
