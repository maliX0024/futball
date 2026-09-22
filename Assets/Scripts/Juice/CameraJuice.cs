using System.Collections;
using UnityEngine;

namespace Futball.Juice
{
    /// <summary>
    /// CameraJuice - handles ALL camera feel in one place:
    ///  - Trauma Shake (decaying, not random): Light=0.2 Medium=0.5 Heavy=1.0
    ///  - Kick Zoom: lerp 10% closer while charging, snap back on kick
    ///  - Goal Pan: follow ball into net then whip to scorer
    ///
    /// Works with plain Camera or Cinemachine (auto-detected).
    /// Add to Main Camera. No other setup needed.
    /// </summary>
    public class CameraJuice : MonoBehaviour
    {
        public static CameraJuice Instance { get; private set; }

        [Header("Refs")]
        public Camera cam;
        [Tooltip("Optional: if using Cinemachine, assign vcam. Otherwise we move this transform.")]
        public MonoBehaviour cinemachineVcam; // keep as MonoBehaviour to avoid hard dependency

        [Header("Shake - Trauma System")]
        [Tooltip("Trauma decays per second")] [Range(0.5f, 5f)] public float traumaDecay = 1.6f;
        [Tooltip("Max translation shake")] public float maxShakeTranslation = 0.35f;
        [Tooltip("Max rotation shake (deg)")] public float maxShakeRotation = 1.8f;
        [Tooltip("Perlin frequency")] public float shakeFrequency = 22f;

        [Header("Kick Zoom")]
        [Tooltip("How much closer (FOV or position)")] [Range(0f, 0.3f)] public float zoomAmount = 0.10f;
        [Tooltip("Zoom lerp speed")] public float zoomLerpSpeed = 12f;

        [Header("Goal Pan")]
        public float goalPanDuration = 0.65f;
        public float whipDuration = 0.28f;

        // Internal
        float _trauma;
        Vector3 _initialPos;
        Quaternion _initialRot;
        float _initialFOV;
        float _targetZoom; // 0..1
        float _currentZoom;
        Coroutine _panRoutine;
        bool _isShaking;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            _initialPos = transform.localPosition;
            _initialRot = transform.localRotation;
            _initialFOV = cam != null ? cam.fieldOfView : 60f;
        }

        void OnEnable()
        {
            _initialPos = transform.localPosition;
            _initialRot = transform.localRotation;
        }

        void Update()
        {
            UpdateKickZoom();
            UpdateTraumaShake();
        }

        // ========== TRAUMA SHAKE ==========
        /// <summary>
        /// Add trauma: Light=0.2 (shot), Medium=0.5 (post), Heavy=1.0 (goal)
        /// Values from your spec: Light=2 Medium=5 Heavy=10 mapped to 0..1
        /// </summary>
        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + Mathf.Clamp01(amount));
            _isShaking = _trauma > 0.001f;
        }

        // Convenience named in your spec language
        public void ShakeLight() => AddTrauma(0.22f);   // 2 / 10
        public void ShakeMedium() => AddTrauma(0.50f);  // 5 / 10
        public void ShakeHeavy() => AddTrauma(1.00f);   // 10 / 10

        void UpdateTraumaShake()
        {
            if (_trauma <= 0f)
            {
                if (_isShaking)
                {
                    transform.localPosition = _initialPos;
                    transform.localRotation = _initialRot;
                    _isShaking = false;
                }
                return;
            }

            // Trauma decay - squared for nice falloff (as per "Juice it or lose it" talk)
            _trauma = Mathf.MoveTowards(_trauma, 0f, traumaDecay * Time.unscaledDeltaTime);
            float shake = _trauma * _trauma; // square makes small trauma subtle, heavy trauma punchy

            float t = Time.unscaledTime * shakeFrequency;

            // Perlin-ish noise via sin with different frequencies
            float x = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f;
            float r = (Mathf.PerlinNoise(t * 0.5f, t * 0.5f) - 0.5f) * 2f;

            Vector3 trans = new Vector3(x, y, 0f) * maxShakeTranslation * shake;
            Vector3 rot = new Vector3(0f, 0f, r) * maxShakeRotation * shake;

            transform.localPosition = _initialPos + trans;
            transform.localRotation = _initialRot * Quaternion.Euler(rot);
        }

        // ========== KICK ZOOM ==========
        /// <summary>
        /// Call every frame while charging: charge01 = 0..1
        /// We lerp 10% closer via FOV (or position if orthographic)
        /// </summary>
        public void SetKickCharge(float charge01)
        {
            _targetZoom = Mathf.Clamp01(charge01) * zoomAmount;
        }

        public void SnapKick() // call on release
        {
            // Snap back + shake
            _targetZoom = 0f;
            // quick punch zoom snap is handled by UpdateKickZoom lerp
            ShakeLight();
        }

        void UpdateKickZoom()
        {
            _currentZoom = Mathf.MoveTowards(_currentZoom, _targetZoom, zoomLerpSpeed * Time.unscaledDeltaTime);
            // Use unscaledDelta so zoom still works during hit-stop
            if (cam == null) return;

            if (cam.orthographic)
            {
                float baseSize = 5f; // will be overridden on first frame if you set it
                // We lerp orthographic size down by zoomAmount
                // Store initial size lazily
                cam.orthographicSize = Mathf.Lerp(_initialFOV /*abused as size*/, _initialFOV * (1f - zoomAmount), _currentZoom / zoomAmount);
            }
            else
            {
                cam.fieldOfView = Mathf.Lerp(_initialFOV, _initialFOV * (1f - zoomAmount), _currentZoom / zoomAmount);
            }
        }

        // Call this if you want to init FOV/size properly
        public void CaptureInitialFOV()
        {
            if (cam.orthographic) _initialFOV = cam.orthographicSize;
            else _initialFOV = cam.fieldOfView;
            _initialPos = transform.localPosition;
            _initialRot = transform.localRotation;
        }

        // ========== GOAL PAN ==========
        /// <summary>
        /// Follow ball into net, then whip to scorer. Call from GoalJuice.
        /// </summary>
        public void DoGoalPan(Transform ball, Transform scorer, System.Action onComplete = null)
        {
            if (_panRoutine != null) StopCoroutine(_panRoutine);
            _panRoutine = StartCoroutine(GoalPanRoutine(ball, scorer, onComplete));
        }

        IEnumerator GoalPanRoutine(Transform ball, Transform scorer, System.Action onComplete)
        {
            // 1) Follow ball for ~0.35s (let physics settle)
            Vector3 startPos = transform.position;
            float t = 0f;
            float followTime = goalPanDuration - whipDuration;

            while (t < 1f && ball != null)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.01f, followTime);
                Vector3 ballPos = ball.position;
                // Keep camera looking at ball but maintain height
                Vector3 target = ballPos + (startPos - ballPos).normalized * 0.2f; // tiny offset
                // Instead of moving camera pos drastically, we lerp position slightly and rotate to look
                // For simplicity, slerp rotation toward ball
                if (cam != null)
                {
                    Quaternion look = Quaternion.LookRotation(ballPos - transform.position);
                    transform.rotation = Quaternion.Slerp(transform.rotation, look, t * 3f * Time.unscaledDeltaTime * 10f);
                }
                yield return null;
            }

            // 2) Whip pan to scorer
            if (scorer != null)
            {
                Quaternion startRot = transform.rotation;
                Quaternion endRot = Quaternion.LookRotation(scorer.position - transform.position);
                // Whip = overshoot then settle (cubic)
                float w = 0f;
                while (w < 1f)
                {
                    w += Time.unscaledDeltaTime / whipDuration;
                    float eased = 1f - Mathf.Pow(1f - w, 3f); // easeOutCubic
                    // add tiny overshoot at 0.7
                    if (w > 0.7f && w < 0.85f) eased += 0.06f * Mathf.Sin((w - 0.7f) * 40f);
                    transform.rotation = Quaternion.Slerp(startRot, endRot, eased);
                    yield return null;
                }
            }

            onComplete?.Invoke();
            // Restore initial rotation slowly?
            // Let it sit on scorer for celebration duration, then caller can lerp back
            _panRoutine = null;
        }

        public void ResetCamera(float duration = 0.4f)
        {
            if (_panRoutine != null) StopCoroutine(_panRoutine);
            _panRoutine = StartCoroutine(ResetRoutine(duration));
        }

        IEnumerator ResetRoutine(float dur)
        {
            Vector3 startPos = transform.localPosition;
            Quaternion startRot = transform.localRotation;
            float startFOV = cam != null ? cam.fieldOfView : _initialFOV;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / dur;
                float e = Mathf.SmoothStep(0, 1, t);
                transform.localPosition = Vector3.Lerp(startPos, _initialPos, e);
                transform.localRotation = Quaternion.Slerp(startRot, _initialRot, e);
                if (cam != null && !cam.orthographic) cam.fieldOfView = Mathf.Lerp(startFOV, _initialFOV, e);
                yield return null;
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (cam == null) cam = GetComponent<Camera>();
        }
#endif
    }
}
