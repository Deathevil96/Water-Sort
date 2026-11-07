using UnityEngine;
using UnityEngine.UI;

namespace PaintSort.Game
{
    /// <summary>The device classes the UI adapts to. Phone and tablet portrait share a layout; landscape (tablet, desktop, Android TV) has its own.</summary>
    public enum DeviceClass { Phone, Tablet, Tv }

    /// <summary>
    /// Responsive layout. The canvas is scaled so the logical size is always at least the design size (1080x1920 portrait / 1920x1080 landscape),
    /// content lives inside a safe-area root (notches, rounded corners, TV overscan), and <see cref="Relayout"/> repositions the screens
    /// for the current shape. Nothing here depends on a fixed resolution.
    /// </summary>
    public sealed partial class App
    {
        // design sizes (logical canvas units)
        static readonly Vector2 PortraitDesign = new Vector2(1080, 1920), LandscapeDesign = new Vector2(1920, 1080);
        const float TopBarHeight = 110f, TopBarMargin = 16f;
        const float SidePanelWidth = 560f;                 // landscape: power-up column on the right
        const float MenuHeroHeight = 760f, MenuActionsHeight = 620f, MenuDesignWidth = 1080f;

        public DeviceClass Device { get; private set; }
        public bool IsLandscape { get; private set; }
        /// <summary>Logical size (canvas units) of the safe area content is laid out in.</summary>
        public Vector2 SafeSize { get; private set; }

        Image backdropImage; Sprite backdropPortrait, backdropLandscape;
        float bannerH = 120f;
        Vector2 boardSize;
        int lastW, lastH; Rect lastSafe;
        bool forceTv;

        RectTransform[] powerBtnRt = new RectTransform[4], powerLabelRt = new RectTransform[4], powerChipRt = new RectTransform[4];

        // ------------------------------------------------------------------ device detection + orientation policy
        void DetectDevice()
        {
            Device = forceTv || DetectAndroidTv() ? DeviceClass.Tv : IsTabletSized() ? DeviceClass.Tablet : DeviceClass.Phone;
            if (Device == DeviceClass.Tv) InputMode.FocusVisible = true;               // a TV remote has no pointer: show focus from the start
            switch (Device)
            {
                case DeviceClass.Tv: Screen.orientation = ScreenOrientation.LandscapeLeft; break;
                case DeviceClass.Tablet:
                    Screen.autorotateToPortrait = Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = true; Screen.autorotateToPortraitUpsideDown = false;
                    Screen.orientation = ScreenOrientation.AutoRotation; break;
                default: Screen.orientation = ScreenOrientation.Portrait; break;
            }
            Log.I("LAYOUT", "device=" + Device + " " + Screen.width + "x" + Screen.height + " dpi=" + Screen.dpi);
        }

        static bool IsTabletSized()
        {
            if (Application.isEditor || Screen.dpi < 1f) return false;                 // unknown density: assume phone
            float diagonalInches = Mathf.Sqrt(Screen.width * (float)Screen.width + Screen.height * (float)Screen.height) / Screen.dpi;
            return diagonalInches >= 7f;
        }

        static bool DetectAndroidTv()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var uiMode = activity.Call<AndroidJavaObject>("getSystemService", "uimode"))
                    return uiMode.Call<int>("getCurrentModeType") == 4;               // Configuration.UI_MODE_TYPE_TELEVISION
            }
            catch (System.Exception e) { Log.W("LAYOUT", "TV detection failed: " + e.Message); }
#endif
            return false;
        }

        // ------------------------------------------------------------------ polling
        /// <summary>Cheap per-frame check; the expensive work only runs when the window size, orientation or safe area actually changed.</summary>
        void PollScreen()
        {
            if (Screen.width == lastW && Screen.height == lastH && ReadSafeArea() == lastSafe) return;
            ApplyScreen();
        }

        /// <summary>
        /// Safe area as fractions of the screen (0..1). Uses UnityEngine.Device.Screen so the editor's Device Simulator reports the safe area and the
        /// screen size in the same coordinate space (on a real device it is identical to Screen). Implausible values fall back to the full screen.
        /// </summary>
        static Rect ReadSafeArea()
        {
            float w = Mathf.Max(1, UnityEngine.Device.Screen.width), h = Mathf.Max(1, UnityEngine.Device.Screen.height);
            var sa = UnityEngine.Device.Screen.safeArea;
            float x0 = Mathf.Clamp01(sa.xMin / w), x1 = Mathf.Clamp01(sa.xMax / w), y0 = Mathf.Clamp01(sa.yMin / h), y1 = Mathf.Clamp01(sa.yMax / h);
            if (x1 - x0 < 0.5f || y1 - y0 < 0.5f) return new Rect(0, 0, 1, 1);
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        void ApplyScreen()
        {
            lastW = Screen.width; lastH = Screen.height; lastSafe = ReadSafeArea();
            if (lastW < 2 || lastH < 2) return;
            IsLandscape = lastW > lastH;

            // 1) canvas scaler: match whichever axis keeps the whole design visible, so logical size >= design size on both axes
            var design = IsLandscape ? LandscapeDesign : PortraitDesign;
            float aspect = lastW / (float)lastH, designAspect = design.x / design.y;
            scaler.referenceResolution = design;
            scaler.matchWidthOrHeight = aspect < designAspect ? 0f : 1f;
            float scale = aspect < designAspect ? lastW / design.x : lastH / design.y;

            // 2) safe area (notch / rounded corners / TV overscan) as anchors, so every screen is inset consistently
            var sa = lastSafe;                                                  // fractions of the screen
            safeRoot.anchorMin = sa.min; safeRoot.anchorMax = sa.max;
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            SafeSize = new Vector2(sa.width * lastW / scale, sa.height * lastH / scale);
            LayoutBackdrop(new Vector2(lastW, lastH) / scale);
            LayoutSplash(new Vector2(lastW, lastH) / scale);

            Relayout();
        }

        /// <summary>Scales the supplied backdrop picture to cover the whole screen (cropping the overflow) instead of stretching it; landscape uses its own picture when there is one.</summary>
        void LayoutBackdrop(Vector2 screenLogical)
        {
            if (backdropImage == null) return;
            var sprite = IsLandscape && backdropLandscape != null ? backdropLandscape : backdropPortrait;
            backdropImage.sprite = sprite;
            var rt = backdropImage.rectTransform;
            float cover = Mathf.Max(screenLogical.x / sprite.rect.width, screenLogical.y / sprite.rect.height);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = sprite.rect.size * cover;
        }

        // ------------------------------------------------------------------ layout
        void Relayout()
        {
            if (gameScreen == null) return;
            float W = SafeSize.x, H = SafeSize.y;
            bannerH = Device == DeviceClass.Tv ? 0f : IsLandscape ? 90f : 120f;
            bannerRoot.gameObject.SetActive(bannerH > 0f);
            bannerRoot.anchorMin = Vector2.zero; bannerRoot.anchorMax = new Vector2(1, 0);
            bannerRoot.offsetMin = Vector2.zero; bannerRoot.offsetMax = new Vector2(0, bannerH);

            if (!IsLandscape) LayoutPortrait(W, H); else LayoutLandscape(W, H);
            LayoutPowerSlots(IsLandscape);
            // the shuffle bar takes the power bar's exact place while shuffling, so it can never cover a pot
            shuffleBar.anchorMin = powerBar.anchorMin; shuffleBar.anchorMax = powerBar.anchorMax; shuffleBar.pivot = powerBar.pivot;
            shuffleBar.anchoredPosition = powerBar.anchoredPosition; shuffleBar.sizeDelta = powerBar.sizeDelta;
            if (Board != null) Board.SetArea(boardSize);

            foreach (var p in popups) if (p != null) FitPopup((RectTransform)p.transform.GetChild(1));
            Log.I("LAYOUT", (IsLandscape ? "landscape " : "portrait ") + Mathf.RoundToInt(W) + "x" + Mathf.RoundToInt(H) + " board " + Mathf.RoundToInt(boardSize.x) + "x" + Mathf.RoundToInt(boardSize.y));
        }

        void LayoutPortrait(float W, float H)
        {
            UI.Place(titleRow, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1000, 110));
            SetBoardRect(new Vector2(10, bannerH + 330), new Vector2(-10, -280));
            UI.Place(powerBar, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, bannerH + 20), new Vector2(1040, 290));
            UI.Place(hintBar, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, bannerH + 320), new Vector2(980, 170));
            UI.Place(toastCard, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, bannerH + 350), new Vector2(940, 130));

            // main menu: hero (logo + pots) in the upper part, actions in the lower part; both stay centred on wide tablets
            PlacePanel(menuHero, new Vector2(0.5f, 0.74f), new Vector2(MenuDesignWidth, MenuHeroHeight), Vector2.zero, Mathf.Min(1f, W / MenuDesignWidth));
            PlacePanel(menuActions, new Vector2(0.5f, 0.33f), new Vector2(MenuDesignWidth, MenuActionsHeight), Vector2.zero, Mathf.Min(1f, W / MenuDesignWidth));
        }

        void LayoutLandscape(float W, float H)
        {
            // title sits inside the top-bar band, between the left cluster (back / lives / gold) and the settings button
            UI.Place(titleRow, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(212, -TopBarMargin - 5), new Vector2(1000, 100));
            float boardW = W - SidePanelWidth - 40f, boardCenterX = 20f + boardW * 0.5f;
            SetBoardRect(new Vector2(20, bannerH + 20), new Vector2(-(SidePanelWidth + 20), -(TopBarMargin + TopBarHeight + 14)));
            UI.Place(powerBar, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, (bannerH - 120f) * 0.5f), new Vector2(540, 620));
            UI.Place(hintBar, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(boardCenterX, bannerH + 20), new Vector2(Mathf.Min(1100, boardW - 20), 150));
            UI.Place(toastCard, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(boardCenterX, bannerH + 40), new Vector2(Mathf.Min(940, boardW - 20), 130));

            // main menu: two columns, each scaled to fit its half
            float bandH = H - (TopBarMargin + TopBarHeight) - bannerH - 20f, halfW = W * 0.5f - 40f;
            float cy = (bannerH - (TopBarMargin + TopBarHeight)) * 0.5f;
            PlacePanel(menuHero, new Vector2(0.25f, 0.5f), new Vector2(MenuDesignWidth, MenuHeroHeight), new Vector2(0, cy), Mathf.Min(1f, halfW / MenuDesignWidth, bandH / MenuHeroHeight));
            PlacePanel(menuActions, new Vector2(0.75f, 0.5f), new Vector2(MenuDesignWidth, MenuActionsHeight), new Vector2(0, cy), Mathf.Min(1f, halfW / MenuDesignWidth, bandH / MenuActionsHeight));
        }

        /// <summary>Anchors the board rectangle with explicit offsets and records its logical size for the pot layout.</summary>
        void SetBoardRect(Vector2 offsetMin, Vector2 offsetMax)
        {
            boardRect.anchorMin = Vector2.zero; boardRect.anchorMax = Vector2.one;
            boardRect.offsetMin = offsetMin; boardRect.offsetMax = offsetMax;
            boardSize = new Vector2(Mathf.Max(100f, SafeSize.x + offsetMax.x - offsetMin.x), Mathf.Max(100f, SafeSize.y + offsetMax.y - offsetMin.y));
        }

        static void PlacePanel(RectTransform r, Vector2 anchor, Vector2 size, Vector2 pos, float scale)
        {
            UI.Place(r, anchor, new Vector2(0.5f, 0.5f), pos, size);
            r.localScale = Vector3.one * scale;
        }

        /// <summary>Power-up buttons: one row under the board in portrait, a 2x2 grid beside it in landscape.</summary>
        void LayoutPowerSlots(bool landscape)
        {
            for (int slot = 0; slot < 4; slot++)
            {
                float x = landscape ? 135f + (slot % 2) * 270f : 125f + slot * 263f;
                float y = landscape ? -(slot / 2) * 310f : 0f;
                SetSlot(powerBtnRt[slot], x, y);
                SetSlot(powerLabelRt[slot], x, y - 178f);
                if (powerChipRt[slot] != null) SetSlot(powerChipRt[slot], x, y - 226f);
            }
        }

        static void SetSlot(RectTransform r, float x, float y) { r.anchoredPosition = new Vector2(x, y); }

        /// <summary>Shrinks a popup card so it always fits the visible area (tall popups on landscape screens, short windows).</summary>
        void FitPopup(RectTransform card)
        {
            if (card == null) return;
            var size = card.sizeDelta;
            float s = Mathf.Max(0.3f, Mathf.Min(1f, (SafeSize.x - 40f) / size.x, (SafeSize.y - 40f) / size.y));
            var pop = card.GetComponent<PopupCard>();
            if (pop != null) { pop.Fit = s; pop.Apply(); } else card.localScale = Vector3.one * s;
        }
    }
}
