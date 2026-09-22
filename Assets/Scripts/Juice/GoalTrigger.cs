using UnityEngine;
using Futball.Audio;

namespace Futball.Juice
{
    /// <summary>
    /// Put on Goal trigger collider (IsTrigger = true).
    /// Detects ball entering, fires FULL juice sequence.
    /// Tags: Ball should be tagged "Ball"
    /// </summary>
    public class GoalTrigger : MonoBehaviour
    {
        [Header("Setup")]
        public string ballTag = "Ball";
        public Transform ballTransform;
        public Transform scorer; // assign striker, or auto-find closest player
        public Color teamColor = new Color(0.15f, 0.6f, 1f); // set per goal
        public NetJuice net;

        [Header("Cooldown")]
        public float cooldown = 2.5f;
        bool _onCooldown;

        void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (_onCooldown) return;
            if (!other.CompareTag(ballTag)) return;

            Transform ball = ballTransform != null ? ballTransform : other.transform;
            // Try to find scorer as closest player in last 1.5s - simplified: assigned scorer
            if (scorer == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) scorer = player.transform;
            }

            // Fire sequence
            if (GoalJuiceSequence.Instance != null)
                GoalJuiceSequence.Instance.PlayGoal(ball.position, teamColor, scorer);
            else
            {
                // Fallback juice without sequence manager
                if (TimeController.Instance) TimeController.Instance.DoHitStop(0.07f);
                if (CameraJuice.Instance) CameraJuice.Instance.ShakeHeavy();
                if (net) net.DoStretch(1f);
                if (AudioManager.Instance) AudioManager.Instance.PlayGoalSequence();
                Haptics.Heavy();
            }

            // Also tell ball to do net SFX via Audio (hard vs soft based on speed)
            var rb = other.GetComponent<Rigidbody>();
            bool hard = rb != null && rb.velocity.magnitude > 8f;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayNet(hard);

            _onCooldown = true;
            Invoke(nameof(ResetCooldown), cooldown);
        }

        void ResetCooldown() => _onCooldown = false;

        // Support 2D physics too
        void OnTriggerEnter2D(Collider2D other)
        {
            if (_onCooldown) return;
            if (!other.CompareTag(ballTag)) return;
            Transform ball = ballTransform != null ? ballTransform : other.transform;
            if (GoalJuiceSequence.Instance != null)
                GoalJuiceSequence.Instance.PlayGoal(ball.position, teamColor, scorer);
            _onCooldown = true;
            Invoke(nameof(ResetCooldown), cooldown);
        }
    }
}
