using System.Collections;
using UnityEngine;

namespace Futball.Juice
{
    /// <summary>
    /// Central time control: HitStop (60-80ms freeze) + SlowMo.
    /// Uses unscaled time so it works even when Time.timeScale = 0.
    /// Attach to a GameObject in first scene (e.g. _Managers).
    /// </summary>
    public class TimeController : MonoBehaviour
    {
        public static TimeController Instance { get; private set; }

        [Header("Defaults")]
        [Tooltip("Default hit stop for GOAL")] public float goalHitStopDuration = 0.07f;
        [Tooltip("Default slow-mo factor")] public float slowMoFactor = 0.3f;
        [Tooltip("Default slow-mo duration")] public float slowMoDuration = 0.5f;

        private Coroutine _routine;
        private float _defaultFixedDelta;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _defaultFixedDelta = Time.fixedDeltaTime;
        }

        // ========== PUBLIC API ==========

        /// <summary>Freeze everything for ~70ms. Call on Goal / Post / Big Save.</summary>
        public void DoHitStop(float durationSeconds = 0.07f)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(HitStopRoutine(durationSeconds));
        }

        /// <summary>Smooth slow-mo for celebrations. e.g. 0.3x for 0.5s</summary>
        public void DoSlowMo(float duration = 0.5f, float timeScale = 0.3f)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(SlowMoRoutine(duration, timeScale));
        }

        // Convenience combos used by GoalJuice
        public void DoHitStopThenSlowMo(float hitStopDur, float slowDur, float slowScale)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(HitStopThenSlowMo(hitStopDur, slowDur, slowScale));
        }

        // ========== ROUTINES ==========
        IEnumerator HitStopRoutine(float dur)
        {
            float prevScale = Time.timeScale;
            Time.timeScale = 0f;
            // Keep FixedDelta in sync when we freeze
            Time.fixedDeltaTime = _defaultFixedDelta * Time.timeScale;

            // Use unscaled time - realtimeWait
            yield return new WaitForSecondsRealtime(dur);

            Time.timeScale = prevScale;
            Time.fixedDeltaTime = _defaultFixedDelta * Time.timeScale;
            _routine = null;
        }

        IEnumerator SlowMoRoutine(float dur, float targetScale)
        {
            // Smooth in
            yield return LerpTimeScale(Time.timeScale, targetScale, 0.08f);
            yield return new WaitForSecondsRealtime(dur);
            // Smooth out
            yield return LerpTimeScale(Time.timeScale, 1f, 0.15f);
            _routine = null;
        }

        IEnumerator HitStopThenSlowMo(float hitDur, float slowDur, float slowScale)
        {
            // 1) Hit freeze
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0f;
            yield return new WaitForSecondsRealtime(hitDur);

            // 2) Slow
            Time.timeScale = slowScale;
            Time.fixedDeltaTime = _defaultFixedDelta * slowScale;
            yield return new WaitForSecondsRealtime(slowDur);

            // 3) Restore smoothly
            yield return LerpTimeScale(Time.timeScale, 1f, 0.2f);
            _routine = null;
        }

        IEnumerator LerpTimeScale(float from, float to, float lerpDur)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / lerpDur;
                Time.timeScale = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t));
                Time.fixedDeltaTime = _defaultFixedDelta * Time.timeScale;
                yield return null;
            }
            Time.timeScale = to;
            Time.fixedDeltaTime = _defaultFixedDelta * to;
        }

        void OnDisable()
        {
            // Safety: never leave game frozen in editor
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _defaultFixedDelta;
        }
    }
}
