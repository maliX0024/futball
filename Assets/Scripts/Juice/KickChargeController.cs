using UnityEngine;
using UnityEngine.Events;
using Futball.Audio;
using Futball.Juice;

namespace Futball.Player
{
    /// <summary>
    /// Example: How to wire Kick Zoom + Charge Sound + Kick SFX + Camera shake.
    /// Add to Player, assign ball Rigidbody + camera.
    /// Holds Space / Mouse0 to charge, releases to kick.
    /// Replace input with your New Input System when ready.
    /// </summary>
    public class KickChargeController : MonoBehaviour
    {
        [Header("Refs")]
        public Rigidbody ballRb;
        public Transform kickPoint; // foot position
        public BallJuice ballJuice;

        [Header("Kick Tuning")]
        public float maxChargeTime = 1.1f;
        public float minKickForce = 6f;
        public float maxKickForce = 18f;
        public float upwardLift = 1.2f;
        public AnimationCurve chargeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Events")]
        public UnityEvent<float> onCharge; // 0..1
        public UnityEvent<float> onKick;   // power 0..1

        float _charge;
        bool _charging;
        float _chargeTimer;

        void Update()
        {
            // Demo input - Space or Mouse0
            bool held = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);
            bool released = Input.GetKeyUp(KeyCode.Space) || Input.GetMouseButtonUp(0);
            bool started = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);

            if (started && !_charging)
            {
                _charging = true;
                _chargeTimer = 0f;
                if (AudioManager.Instance != null) AudioManager.Instance.StartCharge();
            }

            if (_charging && held)
            {
                _chargeTimer += Time.deltaTime;
                float raw = Mathf.Clamp01(_chargeTimer / maxChargeTime);
                _charge = chargeCurve.Evaluate(raw);

                // Drive Juice
                if (CameraJuice.Instance != null) CameraJuice.Instance.SetKickCharge(_charge);
                if (AudioManager.Instance != null) AudioManager.Instance.UpdateCharge(_charge);
                onCharge?.Invoke(_charge);

                // Optional wobble if over-charged (adds tension)
                if (_chargeTimer > maxChargeTime)
                {
                    // shake light to signal over-charge
                    if (Random.value < 0.12f && CameraJuice.Instance != null) CameraJuice.Instance.ShakeLight();
                }
            }

            if (_charging && released)
            {
                DoKick(_charge);
                _charging = false;
                _charge = 0f;
                _chargeTimer = 0f;
                if (CameraJuice.Instance != null) CameraJuice.Instance.SnapKick();
                if (AudioManager.Instance != null) AudioManager.Instance.StopCharge();
            }

            // Roll sound every frame
            if (ballRb != null && AudioManager.Instance != null)
                AudioManager.Instance.UpdateRoll(ballRb.velocity.magnitude);
        }

        void DoKick(float power01)
        {
            if (ballRb == null || kickPoint == null) return;
            float force = Mathf.Lerp(minKickForce, maxKickForce, power01);
            Vector3 dir = (ballRb.position - kickPoint.position).normalized;
            // Bias toward camera forward (player aim) - replace with your aim logic
            Vector3 aim = Camera.main ? Camera.main.transform.forward : transform.forward;
            aim.y = 0; aim.Normalize();
            dir = Vector3.Slerp(dir, aim, 0.65f);
            dir += Vector3.up * (upwardLift * Mathf.Lerp(0.2f, 0.6f, power01));
            dir.Normalize();

            ballRb.velocity = Vector3.zero;
            ballRb.AddForce(dir * force, ForceMode.VelocityChange);

            // Juice
            if (ballJuice != null) ballJuice.OnKick(dir, power01);
            else Haptics.OnShot(power01);

            if (AudioManager.Instance != null) AudioManager.Instance.PlayKick(power01);
            if (CameraJuice.Instance != null)
            {
                if (power01 > 0.65f) CameraJuice.Instance.ShakeMedium();
                else CameraJuice.Instance.ShakeLight();
            }

            onKick?.Invoke(power01);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (kickPoint != null && ballRb != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(kickPoint.position, ballRb.position);
                Gizmos.DrawWireSphere(kickPoint.position, 0.18f);
            }
        }
#endif
    }
}
