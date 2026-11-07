using UnityEngine;

namespace PaintSort.Game
{
    /// <summary>
    /// Short, graded vibration pulses. Android uses the system Vibrator with a one-shot effect (API 26+) so a tap feels like a tap,
    /// not the long buzz of <see cref="Handheld.Vibrate"/>. Other platforms do nothing (iOS needs a native plugin for taptic feedback).
    /// </summary>
    public static class Haptics
    {
        public static bool Enabled = true;

        public static void Light() => Pulse(12, 70);       // invalid move, pour landed
        public static void Medium() => Pulse(26, 150);     // a pot was completed
        public static void Heavy() => Pulse(48, 255);      // level complete

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator; static int sdk; static bool initialised, failed;

        static void Init()
        {
            initialised = true;
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                using (var version = new AndroidJavaClass("android.os.Build$VERSION")) sdk = version.GetStatic<int>("SDK_INT");
                if (vibrator != null && !vibrator.Call<bool>("hasVibrator")) vibrator = null;
            }
            catch (System.Exception e) { failed = true; Log.W("HAPTICS", "Vibrator unavailable: " + e.Message); }
        }

        static void Pulse(long milliseconds, int amplitude)
        {
            if (!Enabled || failed) return;
            if (!initialised) Init();
            if (vibrator == null) return;
            try
            {
                if (sdk >= 26)
                    using (var effect = new AndroidJavaClass("android.os.VibrationEffect").CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude))
                        vibrator.Call("vibrate", effect);
                else vibrator.Call("vibrate", milliseconds);
            }
            catch (System.Exception e) { failed = true; Log.W("HAPTICS", "Vibrate failed: " + e.Message); }
        }

        // Referencing Handheld.Vibrate makes Unity's Android build add the VIBRATE permission to the manifest.
        static void PermissionMarker() { if (sdk < 0) Handheld.Vibrate(); }
#else
        static void Pulse(long milliseconds, int amplitude) { }
#endif
    }
}
