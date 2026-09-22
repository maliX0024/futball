using System.Collections;
using UnityEngine;

namespace Futball.Juice
{
    /// <summary>
    /// Net stretch effect - called by GoalJuiceSequence.
    /// Supports 3 modes:
    ///  1) Scale punch (works on any net mesh) - default, zero config
    ///  2) Vertex ripple (if you assign meshFilter) - sine wave
    ///  3) Blendshape (if you have blendshape) - not required
    ///
    /// Add to Net GameObject.
    /// </summary>
    public class NetJuice : MonoBehaviour
    {
        [Header("Scale Punch (always works)")]
        public Vector3 stretchScale = new Vector3(1.25f, 0.85f, 1.25f);
        public float stretchDuration = 0.45f;
        public float wobbleCount = 2f;

        [Header("Vertex Ripple (optional - extra juice)")]
        public MeshFilter meshFilter;
        public float rippleAmplitude = 0.18f;
        public float rippleFrequency = 12f;
        public float rippleDuration = 0.6f;

        Vector3 _initialScale;
        Vector3[] _initialVertices;
        Mesh _mesh;
        Coroutine _routine;

        void Awake()
        {
            _initialScale = transform.localScale;
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                _mesh = meshFilter.mesh; // instance
                _initialVertices = _mesh.vertices;
            }
        }

        public void DoStretch(float power01 = 1f)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(StretchRoutine(power01));
        }

        IEnumerator StretchRoutine(float p)
        {
            // --- Scale punch with spring ---
            float t = 0f;
            Vector3 targetScale = Vector3.Lerp(Vector3.one, stretchScale, p);
            // Convert to world relative
            Vector3 worldTarget = new Vector3(
                _initialScale.x * targetScale.x,
                _initialScale.y * targetScale.y,
                _initialScale.z * targetScale.z
            );

            // Punch out then wobble back using elastic
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / stretchDuration;
                float eased = ElasticOut(t);
                // Wobble decay
                float wobble = Mathf.Sin(t * Mathf.PI * wobbleCount) * (1f - t) * 0.08f;

                Vector3 s = Vector3.LerpUnclamped(_initialScale, worldTarget, 1f - eased);
                s += Vector3.one * wobble;
                transform.localScale = s;

                // Vertex ripple in parallel if mesh exists
                if (_mesh != null && _initialVertices != null)
                {
                    ApplyRipple(t, p);
                }

                yield return null;
            }

            transform.localScale = _initialScale;
            if (_mesh != null) RestoreVertices();
            _routine = null;
        }

        void ApplyRipple(float t, float power)
        {
            Vector3[] verts = new Vector3[_initialVertices.Length];
            float wave = Mathf.Sin(t * rippleFrequency) * rippleAmplitude * power * (1f - t);
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = _initialVertices[i];
                // Displace along normal-ish (z) based on y height
                float heightFactor = Mathf.InverseLerp(-0.5f, 0.5f, v.y);
                verts[i] = v + Vector3.forward * wave * heightFactor;
            }
            _mesh.vertices = verts;
            _mesh.RecalculateNormals();
        }

        void RestoreVertices()
        {
            if (_mesh == null || _initialVertices == null) return;
            _mesh.vertices = _initialVertices;
            _mesh.RecalculateNormals();
        }

        float ElasticOut(float t)
        {
            if (t == 0) return 0;
            if (t == 1) return 1;
            float p = 0.3f;
            return Mathf.Pow(2, -10 * t) * Mathf.Sin((t - p / 4) * (2 * Mathf.PI) / p) + 1;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (meshFilter == null) meshFilter = GetComponentInChildren<MeshFilter>();
        }
        [ContextMenu("Test Stretch")]
        void Test() => DoStretch(1f);
#endif
    }
}
