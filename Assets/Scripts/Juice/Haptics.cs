using UnityEngine;

namespace Futball.Juice
{
    /// <summary>
    /// Haptics wrapper - works on Android/iOS via Handheld + new Vibration API.
    /// Safe to call on any platform (no-ops in Editor/PC).
    /// Usage: Haptics.Light() / Medium() / Heavy() / Selection()
    /// </summary>
    public static class Haptics
    {
        static bool _enabled = true;
        public static bool Enabled { get => _enabled; set => _enabled = value; }

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject _vibrator;
        static bool _initialized;

        static void InitAndroid()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
            } catch { }
        }

        static void VibrateAndroid(long ms, int amplitude = -1)
        {
            if (!_enabled) return;
            InitAndroid();
            if (_vibrator == null) { Handheld.Vibrate(); return; }
            try
            {
                if (AndroidVersion() >= 26 && amplitude >= 0)
                {
                    using (var effect = new AndroidJavaClass("android.os.VibrationEffect")
                        .CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude))
                    {
                        _vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    _vibrator.Call("vibrate", ms);
                }
            } catch { Handheld.Vibrate(); }
        }

        static int AndroidVersion()
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                return version.GetStatic<int>("SDK_INT");
        }
#endif

        public static void Light()
        {
            if (!_enabled) return;
#if UNITY_IOS && !UNITY_EDITOR
            // iOS light impact via _lightImpact if you import iOS Haptics, fallback:
            Handheld.Vibrate();
#elif UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroid(20, 40);
#else
            // Editor/PC - no haptics, maybe log
            // Debug.Log("[HAPTIC] Light");
#endif
        }

        public static void Medium()
        {
            if (!_enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroid(35, 120);
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#else
            // Debug.Log("[HAPTIC] Medium");
#endif
        }

        public static void Heavy()
        {
            if (!_enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroid(60, 255);
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#else
            // Debug.Log("[HAPTIC] Heavy");
#endif
        }

        public static void Selection()
        {
            if (!_enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroid(15, 30);
#else
            // Light tick
#endif
        }

        // Convenience for your spec
        public static void OnShot(float power01)
        {
            if (power01 < 0.33f) Light();
            else if (power01 < 0.66f) Medium();
            else Heavy();
        }
    }
}
