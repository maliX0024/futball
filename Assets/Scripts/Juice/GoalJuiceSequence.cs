using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Futball.Audio;

namespace Futball.Juice
{
    /// <summary>
    /// The full GOAL stack in one call:
    /// Hit stop → Heavy shake → Net stretch → Ball stars → GOAAAL!+crowd → Flash → Slow-mo → Confetti + haptic
    ///
    /// Attach to a GameObject, wire refs in Inspector.
    /// Call: GoalJuiceSequence.Instance.PlayGoal(ball.position, teamColor, scorerTransform);
    /// </summary>
    public class GoalJuiceSequence : MonoBehaviour
    {
        public static GoalJuiceSequence Instance { get; private set; }

        [Header("Refs")]
        public ParticleSystem starBurstPrefab;
        public ParticleSystem confettiPrefab;
        public Image screenFlashImage; // Fullscreen UI Image (white, alpha 0)
        public NetJuice netJuice; // assign net

        [Header("Tuning - GOAL Stack")]
        [Tooltip("Hit stop before shake")] public float hitStopDuration = 0.07f; // 70ms
        [Tooltip("Slow-mo factor")] public float slowMoFactor = 0.32f;
        [Tooltip("Slow-mo duration after hit-stop")] public float slowMoDuration = 0.55f;
        [Tooltip("Flash color - usually team color, alpha handled")] public float flashPeakAlpha = 0.45f;
        public float flashDuration = 0.18f;

        [Header("Ball")]
        public Transform ballTransform;

        [Header("Audio")]
        public bool playGoalAudio = true;

        bool _isPlaying;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (screenFlashImage != null)
            {
                var c = screenFlashImage.color; c.a = 0f; screenFlashImage.color = c;
            }
        }

        /// <summary>
        /// Main entry - call when ball enters goal trigger.
        /// teamColor = scorer's team color for flash.
        /// </summary>
        public void PlayGoal(Vector3 ballPos, Color teamColor, Transform scorer = null)
        {
            if (_isPlaying) return;
            StartCoroutine(GoalRoutine(ballPos, teamColor, scorer));
        }

        // Overload if you don't have color
        public void PlayGoal(Vector3 ballPos, Transform scorer = null)
            => PlayGoal(ballPos, new Color(1f, 0.85f, 0.2f), scorer);

        IEnumerator GoalRoutine(Vector3 ballPos, Color teamColor, Transform scorer)
        {
            _isPlaying = true;

            // Cache
            var time = TimeController.Instance;
            var cam = CameraJuice.Instance;
            var audio = AudioManager.Instance;

            // ===== 1) HIT STOP (freeze 70ms) =====
            // We do hit-stop first - everything frozen, sells impact
            if (time != null) time.DoHitStop(hitStopDuration);
            // During hit-stop we also prep shake so it fires right after
            yield return new WaitForSecondsRealtime(hitStopDuration);

            // ===== 2) HEAVY SHAKE =====
            if (cam != null) cam.ShakeHeavy();

            // ===== 3) NET STRETCH =====
            if (netJuice != null) netJuice.DoStretch(1f);

            // ===== 4) BALL EXPLODES INTO STARS =====
            SpawnStars(ballPos, teamColor);

            // ===== 5) AUDIO: GOAAAL! + Crowd Roar (pitched up) =====
            if (playGoalAudio && audio != null)
            {
                audio.PlayGoalSequence(pitchUp: 1.1f); // pitched up as requested
            }
            else if (audio != null)
            {
                audio.PlaySFX(SoundType.CrowdCheer, pitch: 1.08f, volume: 1f);
            }

            // ===== 6) SCREEN FLASH - team color =====
            if (screenFlashImage != null) StartCoroutine(FlashRoutine(teamColor));

            // ===== 7) SLOW-MO (0.5s at 0.32x) =====
            if (time != null) time.DoSlowMo(slowMoDuration, slowMoFactor);

            // ===== 8) CONFETTI + HAPTIC =====
            SpawnConfetti(ballPos, teamColor);
            Haptics.Heavy();

            // ===== GOAL PAN - camera whips =====
            if (cam != null && ballTransform != null)
            {
                cam.DoGoalPan(ballTransform, scorer);
            }
            else if (cam != null && scorer != null)
            {
                // Fallback: pan from ball pos dummy
                var dummy = new GameObject("GoalBallDummy").transform;
                dummy.position = ballPos;
                cam.DoGoalPan(dummy, scorer, () => Destroy(dummy.gameObject, 1f));
            }

            // Hold celebration window then reset
            yield return new WaitForSecondsRealtime(slowMoDuration + 0.35f);

            if (cam != null) cam.ResetCamera(0.5f);

            _isPlaying = false;
        }

        void SpawnStars(Vector3 pos, Color color)
        {
            if (starBurstPrefab == null) return;
            var ps = Instantiate(starBurstPrefab, pos, Quaternion.identity);
            // Tint to team color
            var main = ps.main;
            main.startColor = color;
            ps.Play();
            Destroy(ps.gameObject, 4f);
        }

        void SpawnConfetti(Vector3 pos, Color teamColor)
        {
            if (confettiPrefab == null) return;
            // Spawn from top, raining down - classic
            Vector3 spawn = pos + Vector3.up * 6f;
            var ps = Instantiate(confettiPrefab, spawn, Quaternion.identity);
            var main = ps.main;
            // Use team color palette + white
            main.startColor = teamColor;
            ps.Play();
            Destroy(ps.gameObject, 4.5f);
        }

        IEnumerator FlashRoutine(Color teamColor)
        {
            if (screenFlashImage == null) yield break;
            teamColor.a = flashPeakAlpha;
            screenFlashImage.color = teamColor;

            float t = 0f;
            // Flash in fast, out slower
            float inDur = flashDuration * 0.25f;
            float outDur = flashDuration * 0.75f;

            // In
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / inDur;
                var c = teamColor; c.a = Mathf.Lerp(0f, flashPeakAlpha, t);
                screenFlashImage.color = c;
                yield return null;
            }
            // Out
            t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / outDur;
                var c = screenFlashImage.color; c.a = Mathf.Lerp(flashPeakAlpha, 0f, Mathf.SmoothStep(0, 1, t));
                screenFlashImage.color = c;
                yield return null;
            }
            var final = screenFlashImage.color; final.a = 0f; screenFlashImage.color = final;
        }

        // ===== Quick test in Editor =====
#if UNITY_EDITOR
        [ContextMenu("TEST Goal Juice")]
        void TestGoal() => PlayGoal(ballTransform ? ballTransform.position : transform.position, Color.cyan, null);
#endif
    }
}
