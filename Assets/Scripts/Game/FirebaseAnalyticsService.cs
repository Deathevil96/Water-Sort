#if PP_FIREBASE && UNITY_ANDROID && !UNITY_EDITOR
using System;
using Firebase;
using Firebase.Analytics;

namespace PaintSort.Game
{
    /// <summary>
    /// Google Analytics for Firebase behind <see cref="IAnalytics"/>. Collection starts OFF (manifest flag added by AndroidFirebasePostProcess)
    /// and is switched on only after the ad consent flow allows it (<see cref="EnableCollection"/>). Events sent before the SDK is ready are dropped.
    /// Compiled only when the PP_FIREBASE define is set (after FirebaseAnalytics.unitypackage is imported) and on Android devices.
    /// </summary>
    public sealed class FirebaseAnalyticsService : IAnalytics
    {
        bool ready, wantCollection;

        public FirebaseAnalyticsService()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(t =>
            {
                if (t.Result != DependencyStatus.Available) { Log.W("ANALYTICS", "Firebase not available: " + t.Result); return; }
                ready = true;
                if (wantCollection) FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            });
        }

        /// <summary>Called once the player's consent choice allows data use.</summary>
        public void EnableCollection()
        {
            wantCollection = true;
            if (ready) FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        }

        public void Event(string name, string detail = null)
        {
            if (!ready) return;
            try
            {
                if (detail == null) FirebaseAnalytics.LogEvent(name);
                else FirebaseAnalytics.LogEvent(name, "detail", detail.Length > 100 ? detail.Substring(0, 100) : detail);
            }
            catch (Exception e) { Log.W("ANALYTICS", "LogEvent failed: " + e.Message); }
        }
    }
}
#endif
