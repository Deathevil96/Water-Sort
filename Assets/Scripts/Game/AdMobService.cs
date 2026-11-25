using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using PaintSort.Core;
using UnityEngine;

namespace PaintSort.Game
{
    /// <summary>
    /// AdMob ad unit ids (app "Palette Pour: Sort Puzzle", app id ca-app-pub-7841394126110372~2192223412).
    /// Editor and development builds use Google's public TEST ids; only release builds get the real ids. Never click your own live ads.
    /// </summary>
    public static class AdIds
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public const string Banner = "ca-app-pub-3940256099942544/6300978111";
        public const string Rewarded = "ca-app-pub-3940256099942544/5224354917";
        public const string Interstitial = "ca-app-pub-3940256099942544/1033173712";
#else
        public const string Banner = "ca-app-pub-7841394126110372/6929865577";
        public const string Rewarded = "ca-app-pub-7841394126110372/4682976188";
        public const string Interstitial = "ca-app-pub-7841394126110372/2519847272";
#endif
    }

    /// <summary>Real AdMob implementation of <see cref="IAdService"/>. Rewarded ads are preloaded; a banner sits at the bottom of the screen.</summary>
    public sealed class AdMobService : IAdService
    {
        readonly AdPolicy policy;
        bool initialized, bannerShown;
        BannerView banner;
        RewardedAd rewarded;
        InterstitialAd interstitial;
        bool loadingRewarded, loadingInterstitial;

        public bool BannerVisible => bannerShown;
        /// <summary>Raised once the consent flow allows data use (the SDK is starting). Analytics collection is switched on here.</summary>
        public event Action ConsentReady;

        public AdMobService(AdPolicy p) { policy = p; }

        /// <summary>Asks for consent where the law requires it (EEA/UK), then starts the SDK.</summary>
        public void Start()
        {
            try
            {
                var req = new ConsentRequestParameters();
                ConsentInformation.Update(req, err =>
                {
                    if (err != null) { Log.W("ADS", "Consent update failed: " + err.Message); InitSdk(); return; }
                    ConsentForm.LoadAndShowConsentFormIfRequired(formErr =>
                    {
                        if (formErr != null) Log.W("ADS", "Consent form: " + formErr.Message);
                        if (ConsentInformation.CanRequestAds()) InitSdk();
                    });
                });
                // a returning player who already gave consent can start at once
                if (ConsentInformation.CanRequestAds()) InitSdk();
            }
            catch (Exception e) { Log.W("ADS", "Start failed: " + e.Message); }
        }

        void InitSdk()
        {
            if (initialized) return;
            initialized = true;
            ConsentReady?.Invoke();
            MobileAds.RaiseAdEventsOnUnityMainThread = true;   // ad callbacks touch Unity UI; without this they arrive on a Java thread
            MobileAds.Initialize(_ =>
            {
                Log.I("ADS", "AdMob initialized");
                ShowBanner(); LoadRewarded(); LoadInterstitial();
            });
        }

        void ShowBanner()
        {
            try
            {
                if (banner != null) banner.Destroy();
                banner = new BannerView(AdIds.Banner, AdSize.Banner, AdPosition.Bottom);
                banner.OnBannerAdLoaded += () => bannerShown = true;
                banner.OnBannerAdLoadFailed += e => { bannerShown = false; Log.W("ADS", "Banner failed: " + e.GetMessage()); };
                banner.LoadAd(new AdRequest());
            }
            catch (Exception e) { Log.W("ADS", "Banner error: " + e.Message); }
        }

        void LoadRewarded()
        {
            if (loadingRewarded || rewarded != null) return;
            loadingRewarded = true;
            RewardedAd.Load(AdIds.Rewarded, new AdRequest(), (ad, err) =>
            {
                loadingRewarded = false;
                if (err != null || ad == null) { Log.W("ADS", "Rewarded load failed: " + (err != null ? err.GetMessage() : "null")); return; }
                rewarded = ad;
            });
        }

        void LoadInterstitial()
        {
            if (loadingInterstitial || interstitial != null) return;
            loadingInterstitial = true;
            InterstitialAd.Load(AdIds.Interstitial, new AdRequest(), (ad, err) =>
            {
                loadingInterstitial = false;
                if (err != null || ad == null) { Log.W("ADS", "Interstitial load failed: " + (err != null ? err.GetMessage() : "null")); return; }
                interstitial = ad;
            });
        }

        public void ShowRewarded(string placement, Action<AdResult> done)
        {
            if (!policy.CooldownReady) { done(AdResult.CooldownActive); return; }
            if (!initialized) { done(AdResult.NoFill); return; }
            if (rewarded == null || !rewarded.CanShowAd()) { LoadRewarded(); done(AdResult.NoFill); return; }

            var ad = rewarded; rewarded = null;
            bool earned = false, finished = false;
            void Finish()
            {
                if (finished) return; finished = true;
                if (earned) policy.MarkShown();
                done(earned ? AdResult.Completed : AdResult.ClosedEarly);
                ad.Destroy(); LoadRewarded();
            }
            ad.OnAdFullScreenContentClosed += () => Finish();
            ad.OnAdFullScreenContentFailed += e =>
            {
                if (finished) return; finished = true;
                Log.W("ADS", "Rewarded show failed: " + e.GetMessage());
                done(AdResult.Failed); ad.Destroy(); LoadRewarded();
            };
            ad.Show(_ => earned = true);
        }

        public void ShowInterstitial(Action done)
        {
            if (!policy.CooldownReady || !initialized || interstitial == null || !interstitial.CanShowAd()) { done?.Invoke(); return; }
            var ad = interstitial; interstitial = null;
            bool finished = false;
            void Finish() { if (finished) return; finished = true; policy.MarkShown(); done?.Invoke(); ad.Destroy(); LoadInterstitial(); }
            ad.OnAdFullScreenContentClosed += Finish;
            ad.OnAdFullScreenContentFailed += e => { if (finished) return; finished = true; done?.Invoke(); ad.Destroy(); LoadInterstitial(); };
            ad.Show();
        }

        /// <summary>Settings > Privacy: lets the player change their consent choice (required by Google wherever the consent form applies).</summary>
        public void ShowPrivacyOptions(Action<string> done)
        {
            try
            {
                if (ConsentInformation.PrivacyOptionsRequirementStatus != PrivacyOptionsRequirementStatus.Required)
                { done?.Invoke("No privacy choices are needed in your region. Ads can be reset in your phone's Google settings."); return; }
                ConsentForm.ShowPrivacyOptionsForm(err =>
                {
                    if (err != null) { Log.W("ADS", "Privacy options form: " + err.Message); done?.Invoke("Could not open privacy options. Please try again."); return; }
                    done?.Invoke(null);
                    if (ConsentInformation.CanRequestAds()) InitSdk();
                });
            }
            catch (Exception e) { Log.W("ADS", "Privacy options failed: " + e.Message); done?.Invoke("Could not open privacy options."); }
        }
    }
}
