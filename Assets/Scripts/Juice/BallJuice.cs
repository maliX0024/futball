using UnityEngine;

namespace Futball.Juice
{
    /// <summary>
    /// Ball Juice - add to Ball prefab (with Rigidbody).
    /// Handles:
    ///  - Squash & stretch on kick / bounce / power
    ///  - Trail length based on speed
    ///  - Roll sound volume via AudioManager
    ///  - Post-hit sparks trigger
    /// Attach to ball, assign trail & squash target (usually ball mesh child).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BallJuice : MonoBehaviour
    {
        [Header("Refs")]
        public Transform visual; // mesh to squash (if null, uses this transform)
        public TrailRenderer trail;
        public ParticleSystem kickBurstPrefab;
        public ParticleSystem postSparkPrefab;

        [Header("Squash & Stretch")]
        [Range(0f, 0.6f)] public float kickSquashAmount = 0.32f;
        [Range(0f, 0.6f)] public float bounceSquashAmount = 0.20f;
        public float squashDuration = 0.18f;
        public float flightStretchFactor = 0.18f; // stretch by velocity

        [Header("Trail")]
        public float minTrailTime = 0.08f;
        public float maxTrailTime = 0.35f;
        public float speedForMaxTrail = 14f;

        Rigidbody _rb;
        Vector3 _initialScale;
        Coroutine _squashRoutine;
        Transform _squashTarget;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _squashTarget = visual != null ? visual : transform;
            _initialScale = _squashTarget.localScale;
            if (trail != null) trail.emitting = false;
        }

        void Update()
        {
            UpdateTrail();
            UpdateFlightStretch();
        }

        void UpdateTrail()
        {
            if (trail == null || _rb == null) return;
            float speed = _rb.velocity.magnitude;
            // Enable trail only when moving fast enough
            bool shouldEmit = speed > 2.5f;
            trail.emitting = shouldEmit;
            if (shouldEmit)
            {
                float t = Mathf.InverseLerp(2.5f, speedForMaxTrail, speed);
                trail.time = Mathf.Lerp(minTrailTime, maxTrailTime, t);
            }
        }

        void UpdateFlightStretch()
        {
            if (_rb == null || _squashRoutine != null) return; // don't fight squash anim
            float speed = _rb.velocity.magnitude;
            if (speed < 1f) return;

            Vector3 velDir = _rb.velocity.normalized;
            float stretch = Mathf.Clamp01(speed / speedForMaxTrail) * flightStretchFactor;

            // Stretch along velocity, squash perpendicular
            // Approximate via scale - assumes ball moves mostly on XZ
            // We lerp scale toward velocity-stretched scale
            Vector3 targetScale = _initialScale;
            // Simple: stretch forward by speed, squash sides slightly
            // For true velocity-aligned stretch, rotate visual to vel - we just scale
            targetScale.x = _initialScale.x * (1f - stretch * 0.5f);
            targetScale.y = _initialScale.y * (1f - stretch * 0.5f);
            targetScale.z = _initialScale.z * (1f + stretch);

            // Smooth
            _squashTarget.localScale = Vector3.Lerp(_squashTarget.localScale, targetScale, Time.deltaTime * 10f);
        }

        // ========== PUBLIC TRIGGERS ==========
        public void OnKick(Vector3 kickDir, float power01)
        {
            // Squash perpendicular to kick, stretch along kick
            Vector3 squash = new Vector3(1f + kickSquashAmount * 0.5f, 1f - kickSquashAmount, 1f + kickSquashAmount * 0.5f);
            // Weight by power
            squash = Vector3.Lerp(Vector3.one, squash, power01);
            DoSquash(squash);

            if (kickBurstPrefab != null)
            {
                var ps = Instantiate(kickBurstPrefab, transform.position, Quaternion.LookRotation(kickDir));
                Destroy(ps.gameObject, 3f);
            }

            Haptics.OnShot(power01);
        }

        public void OnBounce(Collision col)
        {
            float impact = col.relativeVelocity.magnitude;
            if (impact < 1.5f) return;
            float p = Mathf.InverseLerp(1.5f, 12f, impact);
            Vector3 squash = new Vector3(1f + bounceSquashAmount, 1f - bounceSquashAmount, 1f + bounceSquashAmount);
            squash = Vector3.Lerp(Vector3.one, squash, p * 0.7f);
            DoSquash(squash, squashDuration * 0.7f);
        }

        public void OnPostHit(Vector3 hitPoint)
        {
            Vector3 squash = new Vector3(1f - 0.28f, 1f - 0.28f, 1f + 0.4f);
            DoSquash(squash, 0.14f);

            if (postSparkPrefab != null)
            {
                var ps = Instantiate(postSparkPrefab, hitPoint, Quaternion.identity);
                Destroy(ps.gameObject, 2f);
            }

            if (CameraJuice.Instance != null) CameraJuice.Instance.ShakeMedium();
            if (TimeController.Instance != null) TimeController.Instance.DoHitStop(0.06f);
        }

        void DoSquash(Vector3 targetScale3D, float? overrideDur = null)
        {
            if (_squashRoutine != null) StopCoroutine(_squashRoutine);
            _squashRoutine = StartCoroutine(SquashRoutine(targetScale3D, overrideDur ?? squashDuration));
        }

        System.Collections.IEnumerator SquashRoutine(Vector3 target, float dur)
        {
            Vector3 start = _squashTarget.localScale;
            Vector3 squashScale = new Vector3(
                _initialScale.x * target.x,
                _initialScale.y * target.y,
                _initialScale.z * target.z
            );

            float t = 0f;
            float half = dur * 0.35f;
            // Squash in fast
            while (t < 1f)
            {
                t += Time.deltaTime / half;
                _squashTarget.localScale = Vector3.Lerp(start, squashScale, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            // Overshoot + settle with spring
            t = 0f;
            float settleDur = dur * 0.65f;
            Vector3 overshoot = Vector3.LerpUnclamped(_initialScale, squashScale, -0.25f); // slight inverse
            while (t < 1f)
            {
                t += Time.deltaTime / settleDur;
                float eased = 1f - Mathf.Pow(1f - t, 3f); // easeOutCubic
                _squashTarget.localScale = Vector3.Lerp(squashScale, _initialScale, eased);
                // add tiny sine wobble
                if (t < 0.5f) _squashTarget.localScale += Vector3.one * Mathf.Sin(t * 18f) * 0.02f * (1f - t);
                yield return null;
            }
            _squashTarget.localScale = _initialScale;
            _squashRoutine = null;
        }

        void OnCollisionEnter(Collision collision)
        {
            // Auto-detect post hits via tag/layer
            if (collision.gameObject.CompareTag("Post") || collision.gameObject.layer == LayerMask.NameToLayer("Post"))
            {
                OnPostHit(collision.GetContact(0).point);
                if (Futball.Audio.AudioManager.Instance != null)
                    Futball.Audio.AudioManager.Instance.PlaySFX(Futball.Audio.SoundType.PostHit);
            }
            else
            {
                OnBounce(collision);
                // Light bounce sound
                float vol = Mathf.InverseLerp(1f, 10f, collision.relativeVelocity.magnitude);
                if (vol > 0.08f && Futball.Audio.AudioManager.Instance != null)
                    Futball.Audio.AudioManager.Instance.PlaySFX(Futball.Audio.SoundType.Bounce, volume: vol * 0.7f);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Test Kick")]
        void TestKick() => OnKick(transform.forward, 1f);
#endif
    }
}
