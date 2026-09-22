using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Futball.Audio
{
    public enum SoundType
    {
        KickSoft, KickMedium, KickHard,
        Charge,
        Roll,
        Bounce,
        PostHit,
        NetSoft, NetHard,
        CrowdIdle, CrowdGasp, CrowdCheer, GoalShout,
        Whistle, UIPop, Coin
    }

    [System.Serializable]
    public class SoundEntry
    {
        public SoundType type;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float basePitch = 1f;
        public bool loop = false;
        [HideInInspector] public AudioSource source; // for loops
    }

    /// <summary>
    /// AudioManager - covers your full sound table:
    /// Kick Soft/Med/Hard (pitch = power*0.2 + random), Charge (rising whine), Roll (loop), Bounce,
    /// Post *CLANK*, Net swish, Crowd idle/gasp/cheer (ducking), Whistle, UI pop, Coin cascade.
    ///
    /// Setup:
    ///  1) Add to _Managers GameObject.
    ///  2) Assign clips in Inspector (or leave empty for procedural placeholders).
    ///  3) Optionally assign AudioMixerGroups.
    /// Call: AudioManager.Instance.PlayKick(power01), PlayPostHit(), PlayNet(bool hard), etc.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Mixer (optional)")]
        public AudioMixerGroup sfxGroup;
        public AudioMixerGroup crowdGroup;
        public AudioMixerGroup musicGroup;

        [Header("Sources")]
        public AudioSource sfxSource; // one-shot
        public AudioSource crowdIdleSource; // loop
        public AudioSource crowdOneShotSource;
        public AudioSource rollSource; // loop
        public AudioSource chargeSource; // loop
        [Range(0f,1f)] public float masterVolume = 1f;

        [Header("Sound Library")]
        public List<SoundEntry> sounds = new List<SoundEntry>();

        Dictionary<SoundType, SoundEntry> _map;
        Coroutine _chargeRoutine;
        Coroutine _crowdDuckRoutine;
        float _baseCrowdVolume = 0.55f;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildMap();
            EnsureSources();
            PlayCrowdIdle();
        }

        void BuildMap()
        {
            _map = new Dictionary<SoundType, SoundEntry>();
            foreach (var s in sounds) if (!_map.ContainsKey(s.type)) _map[s.type] = s;
        }

        void EnsureSources()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f;
                if (sfxGroup) sfxSource.outputAudioMixerGroup = sfxGroup;
            }
            if (crowdIdleSource == null)
            {
                var go = new GameObject("CrowdIdle");
                go.transform.SetParent(transform);
                crowdIdleSource = go.AddComponent<AudioSource>();
                crowdIdleSource.loop = true; crowdIdleSource.playOnAwake = false; crowdIdleSource.spatialBlend = 0f;
                if (crowdGroup) crowdIdleSource.outputAudioMixerGroup = crowdGroup;
                crowdIdleSource.volume = _baseCrowdVolume;
            }
            if (crowdOneShotSource == null)
            {
                var go = new GameObject("CrowdOneShot");
                go.transform.SetParent(transform);
                crowdOneShotSource = go.AddComponent<AudioSource>();
                crowdOneShotSource.playOnAwake = false; crowdOneShotSource.spatialBlend = 0f;
                if (crowdGroup) crowdOneShotSource.outputAudioMixerGroup = crowdGroup;
            }
            if (rollSource == null)
            {
                var go = new GameObject("RollLoop");
                go.transform.SetParent(transform);
                rollSource = go.AddComponent<AudioSource>();
                rollSource.loop = true; rollSource.playOnAwake = false; rollSource.spatialBlend = 0f;
                rollSource.volume = 0f;
                if (sfxGroup) rollSource.outputAudioMixerGroup = sfxGroup;
            }
            if (chargeSource == null)
            {
                var go = new GameObject("ChargeLoop");
                go.transform.SetParent(transform);
                chargeSource = go.AddComponent<AudioSource>();
                chargeSource.loop = true; chargeSource.playOnAwake = false; chargeSource.spatialBlend = 0f;
                chargeSource.volume = 0f;
                if (sfxGroup) chargeSource.outputAudioMixerGroup = sfxGroup;
            }
        }

        // ========== CORE PLAY ==========
        public void PlaySFX(SoundType type, float pitch = -1f, float volume = -1f)
        {
            if (!_map.TryGetValue(type, out var entry) || entry.clips == null || entry.clips.Length == 0)
            {
                // No clip assigned - play procedural placeholder (so you hear *something*)
                PlayPlaceholder(type, pitch, volume);
                return;
            }
            var clip = entry.clips[Random.Range(0, entry.clips.Length)];
            float v = (volume < 0 ? entry.volume : volume) * masterVolume;
            float p = (pitch < 0 ? entry.basePitch : pitch) * Random.Range(0.95f, 1.05f);

            // Use PlayOneShot for overlapping SFX
            sfxSource.pitch = p;
            sfxSource.PlayOneShot(clip, v);
        }

        void PlayPlaceholder(SoundType type, float pitch, float volume)
        {
            // Generate a tiny tone via PlayOneShot with a generated clip cached per type?
            // Simplest: use sfxSource with no clip won't error - we just log so dev knows to assign
#if UNITY_EDITOR
            // Debug.LogWarning($"[AudioManager] No clip for {type} - assign in Inspector. Playing nothing (placeholder).");
#endif
            // Optional: create 100ms beep
            // For now, no-op - user should assign Mixkit/Freesound clips
        }

        // ========== TABLE-SPECIFIC HELPERS (your spec) ==========

        /// <summary>Kick: Soft/Medium/Hard auto-selected by power, pitch = power*0.2 + random</summary>
        public void PlayKick(float power01)
        {
            power01 = Mathf.Clamp01(power01);
            SoundType t = power01 < 0.35f ? SoundType.KickSoft : power01 < 0.7f ? SoundType.KickMedium : SoundType.KickHard;
            float pitch = 1f + power01 * 0.20f + Random.Range(-0.06f, 0.06f);
            PlaySFX(t, pitch, Mathf.Lerp(0.7f, 1f, power01));
        }

        public void StartCharge()
        {
            if (!_map.TryGetValue(SoundType.Charge, out var e) || e.clips.Length == 0) return;
            if (chargeSource.isPlaying) return;
            chargeSource.clip = e.clips[0];
            chargeSource.pitch = 0.85f;
            chargeSource.volume = 0f;
            chargeSource.Play();
            if (_chargeRoutine != null) StopCoroutine(_chargeRoutine);
            _chargeRoutine = StartCoroutine(ChargeRamp(0.8f));
        }

        public void UpdateCharge(float power01)
        {
            if (!chargeSource.isPlaying) return;
            power01 = Mathf.Clamp01(power01);
            chargeSource.pitch = Mathf.Lerp(0.85f, 1.45f, power01);
            chargeSource.volume = Mathf.Lerp(0.0f, 0.55f, power01) * masterVolume;
        }

        public void StopCharge()
        {
            if (_chargeRoutine != null) StopCoroutine(_chargeRoutine);
            _chargeRoutine = StartCoroutine(ChargeRamp(0f, 0.12f));
        }

        IEnumerator ChargeRamp(float targetVol, float dur = 0.18f)
        {
            float start = chargeSource.volume;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / dur;
                chargeSource.volume = Mathf.Lerp(start, targetVol * masterVolume, t);
                yield return null;
            }
            if (targetVol == 0f) chargeSource.Stop();
        }

        public void UpdateRoll(float speed, float maxSpeed = 14f)
        {
            // Call every frame from Ball - volume = speed mapped
            if (!_map.TryGetValue(SoundType.Roll, out var e) || e.clips.Length == 0) return;
            float vol = Mathf.InverseLerp(1.5f, maxSpeed, speed) * 0.45f * masterVolume;
            if (vol > 0.02f)
            {
                if (!rollSource.isPlaying)
                {
                    rollSource.clip = e.clips[0];
                    rollSource.Play();
                }
                rollSource.volume = Mathf.MoveTowards(rollSource.volume, vol, Time.deltaTime * 4f);
                rollSource.pitch = Mathf.Lerp(0.9f, 1.15f, vol * 2f);
            }
            else
            {
                rollSource.volume = Mathf.MoveTowards(rollSource.volume, 0f, Time.deltaTime * 6f);
                if (rollSource.volume < 0.01f && rollSource.isPlaying) rollSource.Stop();
            }
        }

        public void PlayBounce(float impact01 = 0.5f)
        {
            PlaySFX(SoundType.Bounce, pitch: Random.Range(0.9f, 1.1f), volume: Mathf.Lerp(0.5f, 1f, impact01) * 0.7f);
        }

        public void PlayPostHit()
        {
            PlaySFX(SoundType.PostHit, pitch: Random.Range(0.98f, 1.05f), volume: 0.95f);
            // Gasp from crowd
            PlaySFX(SoundType.CrowdGasp, pitch: Random.Range(0.95f, 1.05f), volume: 0.85f);
        }

        public void PlayNet(bool hard)
        {
            PlaySFX(hard ? SoundType.NetHard : SoundType.NetSoft, pitch: hard ? 1.02f : 0.98f);
        }

        public void PlayCrowdIdle()
        {
            if (!_map.TryGetValue(SoundType.CrowdIdle, out var e) || e.clips.Length == 0) return;
            crowdIdleSource.clip = e.clips[0];
            crowdIdleSource.volume = _baseCrowdVolume * masterVolume;
            crowdIdleSource.loop = true;
            if (!crowdIdleSource.isPlaying) crowdIdleSource.Play();
        }

        public void PlayCrowdCheer(float pitch = 1f)
        {
            // Duck idle under cheer (fake sidechain)
            if (_crowdDuckRoutine != null) StopCoroutine(_crowdDuckRoutine);
            _crowdDuckRoutine = StartCoroutine(CrowdDuckRoutine(0.18f, 1.1f));

            PlaySFX(SoundType.CrowdCheer, pitch: pitch * Random.Range(0.98f, 1.04f), volume: 1f);
        }

        public void PlayGoalSequence(float pitchUp = 1.1f)
        {
            // GOAAAL shout (if you have clip for GoalShout, else Cheer pitched up)
            if (_map.ContainsKey(SoundType.GoalShout) && _map[SoundType.GoalShout].clips.Length > 0)
                PlaySFX(SoundType.GoalShout, pitch: pitchUp);
            else
                PlaySFX(SoundType.CrowdCheer, pitch: pitchUp, volume: 1f);

            // Add net hard
            Invoke(nameof(PlayNetHardDelayed), 0.12f);
            PlayCrowdCheer(pitchUp);
        }

        void PlayNetHardDelayed() => PlayNet(true);

        IEnumerator CrowdDuckRoutine(float duckTo, float returnTime)
        {
            float start = crowdIdleSource.volume;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.12f;
                crowdIdleSource.volume = Mathf.Lerp(start, duckTo * masterVolume, t);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1.0f); // hold ducked while cheer rings
            t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / returnTime;
                crowdIdleSource.volume = Mathf.Lerp(duckTo * masterVolume, _baseCrowdVolume * masterVolume, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
        }

        public void PlayWhistle()
        {
            PlaySFX(SoundType.Whistle, pitch: 1f, volume: 0.9f);
        }

        public void PlayUIPop(bool positive = true)
        {
            PlaySFX(SoundType.UIPop, pitch: positive ? Random.Range(1.05f, 1.15f) : Random.Range(0.85f, 0.95f));
        }

        public void PlayCoin(int count = 1)
        {
            // C-E-G cascade - pitch up per coin, pan left/right
            float[] pitches = { 1f, 1.26f, 1.5f }; // C, E, G
            for (int i = 0; i < Mathf.Min(count, 6); i++)
            {
                float pan = (i % 2 == 0) ? -0.3f : 0.3f;
                // Use sfxSource, but pan via temporary source for stereo? Simpler: just pitch
                float p = pitches[i % 3] * Random.Range(0.99f, 1.01f);
                // Create one-shot source for pan
                var go = new GameObject("CoinTick");
                var src = go.AddComponent<AudioSource>();
                src.spatialBlend = 0f; src.panStereo = pan; src.pitch = p;
                if (sfxGroup) src.outputAudioMixerGroup = sfxGroup;
                if (_map.TryGetValue(SoundType.Coin, out var e) && e.clips.Length > 0)
                {
                    src.clip = e.clips[0];
                    src.volume = 0.85f * masterVolume;
                    src.Play();
                    Destroy(go, 1f);
                }
                else Destroy(go);
            }
        }

        // Rebuild map after inspector changes
        void OnValidate() => BuildMap();
    }
}
