using UnityEngine;
using Futball.Audio;
using Futball.Juice;

namespace Futball.Demo
{
    /// <summary>
    /// Demo tester - attach to empty GameObject, press keys to preview juice without playing match.
    /// G = Goal sequence, P = Post hit, K = Kick, H/J/K = Light/Med/Heavy shake, Space = charge demo
    /// </summary>
    public class FutballJuiceDemo : MonoBehaviour
    {
        [Header("Refs (auto-finds if null)")]
        public Transform ball;
        public Transform scorer;
        public Color teamColor = new Color(0f, 0.75f, 1f);

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                Vector3 pos = ball ? ball.position : transform.position + Vector3.forward * 2f;
                if (GoalJuiceSequence.Instance) GoalJuiceSequence.Instance.PlayGoal(pos, teamColor, scorer);
                Debug.Log("[Demo] GOAL!");
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                Vector3 pos = ball ? ball.position : transform.position;
                var bj = ball ? ball.GetComponent<BallJuice>() : null;
                if (bj) bj.OnPostHit(pos);
                else
                {
                    if (CameraJuice.Instance) CameraJuice.Instance.ShakeMedium();
                    if (TimeController.Instance) TimeController.Instance.DoHitStop(0.06f);
                    if (AudioManager.Instance) AudioManager.Instance.PlayPostHit();
                }
                Debug.Log("[Demo] POST!");
            }
            if (Input.GetKeyDown(KeyCode.K))
            {
                float pow = Random.Range(0f, 1f);
                if (AudioManager.Instance) AudioManager.Instance.PlayKick(pow);
                if (ball) { var bj = ball.GetComponent<BallJuice>(); if (bj) bj.OnKick(Vector3.forward, pow); }
                if (CameraJuice.Instance) CameraJuice.Instance.ShakeLight();
                Debug.Log($"[Demo] KICK pow {pow:F2}");
            }
            if (Input.GetKeyDown(KeyCode.H)) { if (CameraJuice.Instance) CameraJuice.Instance.ShakeLight(); Debug.Log("[Demo] Shake Light"); }
            if (Input.GetKeyDown(KeyCode.J)) { if (CameraJuice.Instance) CameraJuice.Instance.ShakeMedium(); Debug.Log("[Demo] Shake Medium"); }
            if (Input.GetKeyDown(KeyCode.B)) { if (CameraJuice.Instance) CameraJuice.Instance.ShakeHeavy(); Debug.Log("[Demo] Shake Heavy"); }

            // Audio table quick test
            if (Input.GetKeyDown(KeyCode.Alpha1)) AudioManager.Instance?.PlaySFX(SoundType.KickSoft);
            if (Input.GetKeyDown(KeyCode.Alpha2)) AudioManager.Instance?.PlaySFX(SoundType.KickMedium);
            if (Input.GetKeyDown(KeyCode.Alpha3)) AudioManager.Instance?.PlaySFX(SoundType.KickHard);
            if (Input.GetKeyDown(KeyCode.Alpha4)) AudioManager.Instance?.PlaySFX(SoundType.Bounce);
            if (Input.GetKeyDown(KeyCode.Alpha5)) AudioManager.Instance?.PlayPostHit();
            if (Input.GetKeyDown(KeyCode.Alpha6)) AudioManager.Instance?.PlayNet(false);
            if (Input.GetKeyDown(KeyCode.Alpha7)) AudioManager.Instance?.PlayNet(true);
            if (Input.GetKeyDown(KeyCode.Alpha8)) AudioManager.Instance?.PlayWhistle();
            if (Input.GetKeyDown(KeyCode.Alpha9)) AudioManager.Instance?.PlayUIPop(true);
            if (Input.GetKeyDown(KeyCode.Alpha0)) AudioManager.Instance?.PlayCoin(3);
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 340, 400), GUI.skin.box);
            GUILayout.Label("<b>FUTBALL JUICE DEMO</b>");
            GUILayout.Label("G = GOAL full stack");
            GUILayout.Label("P = Post CLANK + shake");
            GUILayout.Label("K = Random Kick");
            GUILayout.Label("H / J / B = Light / Medium / Heavy Shake");
            GUILayout.Label("1-0 = Test sounds (kick/bounce/post/net/whistle/pop/coin)");
            GUILayout.Space(8);
            if (GUILayout.Button("▶ PLAY GOAL (Heavy)")) { Vector3 pos = ball ? ball.position : transform.position; GoalJuiceSequence.Instance?.PlayGoal(pos, teamColor, scorer); }
            if (GUILayout.Button("▶ POST HIT")) { Vector3 pos = ball ? ball.position : transform.position; ball?.GetComponent<BallJuice>()?.OnPostHit(pos); }
            if (GUILayout.Button("▶ RANDOM KICK")) { float p = Random.value; AudioManager.Instance?.PlayKick(p); }
            GUILayout.EndArea();
        }
    }
}
