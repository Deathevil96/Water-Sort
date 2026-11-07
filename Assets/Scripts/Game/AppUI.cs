using System;
using System.Collections;
using System.Collections.Generic;
using PaintSort.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PaintSort.Game
{
    public sealed partial class App
    {
        // ---- scene graph
        Canvas canvas; CanvasScaler scaler;
        RectTransform root, safeRoot, menuScreen, menuHero, menuActions, gameScreen, boardRect, popupLayer, toastLayer, toastCard, topBar, bannerRoot;
        public BoardView Board;

        // hud bits
        Text goldText, livesText, livesSub, levelTitle, movesText, diffText, playLabel, progressText, shufflePrice, potPrice, hintText, toastText;
        Image diffBadge, dailyDot; Button backBtn, playBtn, undoBtn, shuffleBtn, potBtn, restartBtn;
        RectTransform hintBar, shuffleBar, powerBar, titleRow;
        Button shuffleConfirmBtn; Text shuffleInfo;
        List<GameObject> popups;
        List<KeyValuePair<Text, Func<string>>> liveTexts;
        Coroutine toastRoutine, hintRoutine;

        public bool PopupOpen => popups.Count > 0;

        // =====================================================================================  build
        void BuildUi()
        {
            EnsureCamera(); EnsureEventSystem();
            var cgo = new GameObject("PaintSortCanvas"); cgo.transform.SetParent(transform, false);
            canvas = cgo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler = cgo.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // REQUIRED: the root raycaster. Unity only sorts hits from different (nested) canvases by depth when they share a common root raycaster;
            // without it overlapping canvases are ordered arbitrarily and a tap meant for a popup can land on a button behind it.
            // It is cheap (the root canvas only holds the non-interactive backdrop). Covered by the pointer tests in GameFlowTests.
            cgo.AddComponent<GraphicRaycaster>();
            root = (RectTransform)cgo.transform;
            FxHost.Create(root);

            // Backdrop fills the whole screen; everything interactive lives inside the safe-area root.
            BuildBackground();
            safeRoot = UI.Rect(root, "SafeArea"); UI.Stretch(safeRoot);
            menuScreen = UI.Rect(safeRoot, "Menu"); UI.Stretch(menuScreen);
            gameScreen = UI.Rect(safeRoot, "Game"); UI.Stretch(gameScreen);
            BuildMenu(); BuildGame();
            BuildTopBar();
            BuildBanner();
            popupLayer = UI.Rect(root, "Popups"); UI.Stretch(popupLayer);
            toastLayer = UI.Rect(safeRoot, "Toasts"); UI.Stretch(toastLayer);
            BuildToast();
            BuildFade();

            // Separate canvases: a HUD text change rebuilds only the HUD mesh, not the board or the popups.
            UI.SubCanvas(menuScreen); UI.SubCanvas(gameScreen); UI.SubCanvas(topBar); UI.SubCanvas(popupLayer); UI.SubCanvas(toastLayer, false);
            popupLayer.gameObject.SetActive(false);        // only active (and raycasting) while a popup is open
#if UNITY_EDITOR || DEBUG
            BuildDevTools();
#endif
            ApplyScreen();
        }

        void EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null) { var go = new GameObject("Main Camera", typeof(Camera)); go.tag = "MainCamera"; cam = go.GetComponent<Camera>(); }
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Art.Cream; cam.orthographic = true;
        }

        void EnsureEventSystem()
        {
            var es = FindAnyObjectByType<EventSystem>();
            if (es == null) es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            var legacy = es.GetComponent<StandaloneInputModule>(); if (legacy != null) Destroy(legacy);
            if (es.GetComponent<InputSystemUIInputModule>() == null)
            {
                var m = es.gameObject.AddComponent<InputSystemUIInputModule>(); m.AssignDefaultActions();
            }
        }

        void BuildBackground()
        {
            var tex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++) tex.SetPixel(0, y, Color.Lerp(Art.Hex("FFE9C2"), Art.Hex("FFFAEE"), y / 63f));
            tex.Apply(false, true);
            var custom = Resources.Load<Sprite>("Art/backdrop");
            var bg = UI.Img(root, "Backdrop", custom != null ? custom : Sprite.Create(tex, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f), 100), Color.white); UI.Stretch(bg.rectTransform); bg.preserveAspect = false;
            if (custom != null)
            {
                // supplied artwork already has its own decoration; it is scaled to cover the screen without stretching (see LayoutBackdrop)
                backdropImage = bg; backdropPortrait = custom; backdropLandscape = Resources.Load<Sprite>("Art/backdrop_wide");
                return;
            }
            // decorative splats are anchored by percentage, so they spread over any screen shape
            var rng = new DetRandom(11);
            for (int i = 0; i < 9; i++)
            {
                var pc = Art.Paint[i % Art.Paint.Length];
                var s = UI.Img(root, "Splat", Art.Splat, new Color(pc.r, pc.g, pc.b, 0.18f));
                float sz = rng.Range(260, 520);
                UI.Place(s.rectTransform, new Vector2(rng.Range(0, 100) / 100f, rng.Range(0, 100) / 100f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(sz, sz));
                s.rectTransform.localRotation = Quaternion.Euler(0, 0, rng.Range(0, 360));
            }
        }

        RectTransform Pill(Transform parent, Sprite icon, Vector2 size, out Text main, out Text sub)
        {
            // cream pill with a gold edge; the icon sits half over the left end (like the reference HUD), the number is dark and outline-free
            var rt = UI.Rect(parent, "Pill"); rt.sizeDelta = size;
            var sh = UI.Img(rt, "Shadow", Art.RoundSoft, new Color(0.25f, 0.1f, 0.3f, 0.35f), true); UI.Stretch(sh.rectTransform, -3, -10, -3, 4);
            var edge = UI.Img(rt, "Edge", Art.Round, UI.GoldEdge, true); UI.Stretch(edge.rectTransform);
            var rim = UI.Img(rt, "Rim", Art.Round, Art.Hex("B9761F"), true); UI.Stretch(rim.rectTransform, 4, 4, 4, 4);
            var face = UI.Img(rt, "Face", Art.Round, Art.Hex("FFF3DA"), true); UI.Stretch(face.rectTransform, 8, 8, 8, 8);
            var ic = UI.Img(rt, "Icon", icon, Color.white); UI.Place(ic.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(size.y * 0.12f, 2), Vector2.one * size.y * 1.12f);
            var brown = Art.Hex("6B4423");
            main = UI.Txt(rt, "0", 50, brown, TextAnchor.MiddleCenter, false); main.rectTransform.offsetMin = new Vector2(size.y * 0.72f, 6); main.rectTransform.offsetMax = new Vector2(-14, -4);
            sub = UI.Txt(rt, "", 24, brown, TextAnchor.MiddleCenter, false); sub.rectTransform.anchorMin = new Vector2(0, 0); sub.rectTransform.anchorMax = new Vector2(1, 0.4f); sub.rectTransform.offsetMin = new Vector2(size.y * 0.72f, 0); sub.rectTransform.offsetMax = new Vector2(-14, 0);
            return rt;
        }

        void BuildTopBar()
        {
            topBar = UI.Rect(safeRoot, "TopBar");
            UI.Place(topBar, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -TopBarMargin), new Vector2(0, TopBarHeight));
            topBar.anchorMax = new Vector2(1, 1); topBar.offsetMin = new Vector2(0, topBar.offsetMin.y); topBar.offsetMax = new Vector2(0, topBar.offsetMax.y);

            backBtn = UI.Btn(topBar, "", UI.Orange, new Vector2(100, 100), OnBackButton, Art.BackArrow);
            UI.Place((RectTransform)backBtn.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(100, 100));

            var lp = Pill(topBar, Art.Heart, new Vector2(250, 92), out livesText, out livesSub);
            UI.Place(lp, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(168, 0), new Vector2(250, 92));
            var gp = Pill(topBar, Art.Coin, new Vector2(330, 92), out goldText, out _);
            UI.Place(gp, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(456, 0), new Vector2(330, 92));
            settingsBtn = UI.Btn(topBar, "", UI.Blue, new Vector2(100, 100), OpenSettings, Art.Gear);
            UI.Place((RectTransform)settingsBtn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(100, 100));
        }

        void BuildBanner()
        {
            bannerRoot = UI.Rect(safeRoot, "BannerAd");
            var bg = UI.Img(bannerRoot, "Bg", Art.White, new Color(0.15f, 0.1f, 0.22f, 0.9f), false, true); UI.Stretch(bg.rectTransform); bg.preserveAspect = false;
            UI.Txt(bannerRoot, "BANNER AD AREA  (test placeholder — reserved space)", 30, new Color(1, 1, 1, 0.8f), TextAnchor.MiddleCenter, false);
        }

        void BuildToast()
        {
            var inner = UI.Card(toastLayer, Art.Hex("FFFFFF"), new Vector2(940, 130), 6);
            toastCard = (RectTransform)inner.parent; toastCard.name = "ToastCard";
            toastCard.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            toastText = UI.Txt(inner, "", 36, UI.Ink, TextAnchor.MiddleCenter, false); toastText.horizontalOverflow = HorizontalWrapMode.Wrap; toastText.rectTransform.offsetMin = new Vector2(16, 0); toastText.rectTransform.offsetMax = new Vector2(-16, 0);
            toastCard.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- menu
        // The menu is two design-sized panels (hero: logo + pots, actions: play + shortcuts). Relayout() stacks them in portrait and
        // puts them side by side in landscape, so nothing here depends on the screen shape.
        void BuildMenu()
        {
            menuHero = UI.Rect(menuScreen, "Hero"); menuActions = UI.Rect(menuScreen, "Actions");
            menuHero.sizeDelta = new Vector2(MenuDesignWidth, MenuHeroHeight); menuActions.sizeDelta = new Vector2(MenuDesignWidth, MenuActionsHeight);

            var top = new Vector2(0.5f, 1f);
            // placeholder wordmark (replaced by Resources/Art/logo.png when it exists): "PAINT" big, "MAGIC SORT" smaller underneath
            var logo = UI.Txt(menuHero, "<color=#E84C5B>P</color><color=#399CE0>A</color><color=#F5CE45>I</color><color=#54C99B>N</color><color=#9867D5>T</color>\n<size=100><color=#F58D67>M</color><color=#E88DC3>A</color><color=#35C6C5>G</color><color=#B5A1E8>I</color><color=#E84C5B>C</color> <color=#399CE0>S</color><color=#F5CE45>O</color><color=#54C99B>R</color><color=#9867D5>T</color></size>", 190, Color.white);
            logo.supportRichText = true; logo.lineSpacing = 0.9f;
            UI.Place(logo.rectTransform, top, top, new Vector2(0, -40), new Vector2(1000, 420));
            var logoSprite = Resources.Load<Sprite>("Art/logo");
            if (logoSprite != null)
            {
                logo.gameObject.SetActive(false);
                var li = UI.Img(menuHero, "Logo", logoSprite, Color.white); UI.Place(li.rectTransform, top, top, new Vector2(0, -40), new Vector2(1000, 420));
            }
            // decorative pots with real paint layers
            int[][] demo = { new[] { 1, 0, 2, 3 }, new[] { 4, 4, 5 }, new[] { 6, 8, 8, 1 } };
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("DemoPot" + i, typeof(RectTransform)); go.transform.SetParent(menuHero, false);
                var pv = go.AddComponent<PotView>(); pv.Build(4); pv.GetComponent<Image>().raycastTarget = false;
                var units = new List<UnitVis>(); foreach (var c in demo[i]) units.Add(new UnitVis(c, false));
                pv.ShowUnits(units);
                var rt = (RectTransform)go.transform; rt.localScale = Vector3.one * 0.62f;
                UI.Place(rt, top, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 270, -(560 + (i == 1 ? -30 : 0))), rt.sizeDelta);
                pv.SetTilt((i - 1) * -5f);
            }

            playBtn = UI.GlossBtn(menuActions, "", Art.Hex("52C62D"), new Vector2(700, 190), OnPlay);
            UI.Place((RectTransform)playBtn.transform, top, top, new Vector2(0, 0), new Vector2(700, 190));
            playLabel = UI.Txt(playBtn.transform, "Level 1", 100, Color.white); playLabel.rectTransform.offsetMin = new Vector2(30, 14); playLabel.rectTransform.offsetMax = new Vector2(-30, 0);
            playLabel.horizontalOverflow = HorizontalWrapMode.Wrap; playLabel.verticalOverflow = VerticalWrapMode.Truncate;
            playLabel.resizeTextForBestFit = true; playLabel.resizeTextMaxSize = 100; playLabel.resizeTextMinSize = 36;
            { var ol = playLabel.GetComponent<Outline>(); ol.effectColor = Art.Hex("1F5A12"); ol.effectDistance = new Vector2(8, -8); }
            progressText = UI.Txt(menuActions, "", 38, UI.Ink, TextAnchor.MiddleCenter, false);
            UI.Place(progressText.rectTransform, top, top, new Vector2(0, -235), new Vector2(900, 60));

            // icon row
            var row = UI.Rect(menuActions, "Row"); UI.Place(row, top, top, new Vector2(0, -360), new Vector2(980, 170));
            string[] names = { "Levels", "Daily", "Awards", "Ranks", "Shop" };
            Sprite[] icons = { Art.StarIcon, Art.Gift, Art.Medal, Art.Trophy, Art.Bag };
            Action[] acts = { () => OpenLevelSelect(-1), OpenDaily, OpenAchievements, OpenLeaderboard, OpenShop };
            Color[] cols = { UI.Green, UI.Pink, UI.Purple, UI.Orange, UI.Blue };
            for (int i = 0; i < 5; i++)
            {
                float x = -400 + i * 200;
                var b = UI.Btn(row, "", cols[i], new Vector2(170, 140), acts[i], icons[i]);
                UI.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 20), new Vector2(170, 140));
                var lab = UI.Txt(row, names[i], 30, UI.Ink, TextAnchor.MiddleCenter, false); UI.Place(lab.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, -68), new Vector2(190, 40));
                if (i == 1)
                {
                    dailyDot = UI.Img(b.transform, "Dot", Art.Circle, UI.Red); UI.Place(dailyDot.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-10, -6), new Vector2(40, 40));
                }
            }
        }

        // ---------------------------------------------------------------- game screen
        void BuildGame()
        {
            titleRow = UI.Rect(gameScreen, "TitleRow");
            levelTitle = UI.Txt(titleRow, "Level 1", 76, Color.white); levelTitle.rectTransform.anchorMax = new Vector2(0.6f, 1);
            diffBadge = UI.Img(titleRow, "Badge", Art.Round, UI.Green, true); UI.Place(diffBadge.rectTransform, new Vector2(0.6f, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(240, 64));
            var ob = UI.Img(diffBadge.transform, "Ol", Art.Round, UI.Ink, true); UI.Stretch(ob.rectTransform); ob.transform.SetAsFirstSibling(); UI.Stretch(diffBadge.rectTransform, 0, 0, 0, 0);
            UI.Place(diffBadge.rectTransform, new Vector2(0.62f, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(230, 62));
            ob.rectTransform.offsetMin = new Vector2(-5, -5); ob.rectTransform.offsetMax = new Vector2(5, 5);
            diffText = UI.Txt(diffBadge.transform, "Normal", 34, Color.white);
            movesText = UI.Txt(titleRow, "Moves 0", 36, UI.Ink, TextAnchor.MiddleRight, false); movesText.rectTransform.anchorMin = new Vector2(0.8f, 0);

            boardRect = UI.Rect(gameScreen, "Board");
            UI.SubCanvas(boardRect);                      // pots animate; keep their rebuilds away from the HUD
            Board = gameScreen.gameObject.AddComponent<BoardView>(); Board.Init(boardRect); Board.PotTappedCallback = TapPot;

            // power-up bar (slot positions are set by LayoutPowerSlots)
            powerBar = UI.Rect(gameScreen, "PowerBar");
            undoBtn = PowerBtn(0, "Undo", UI.Blue, Art.Undo, OnUndo, null, out _);
            shuffleBtn = PowerBtn(1, "Shuffle", UI.Purple, Art.Shuffle, OnShuffleButton, "", out shufflePrice);
            potBtn = PowerBtn(2, "+ Pot", UI.Green, Art.Plus, OnExtraPotButton, "", out potPrice);
            restartBtn = PowerBtn(3, "Restart", UI.Orange, Art.Restart, OnRestartButton, null, out _);

            // shuffle selection bar
            var sbInner = UI.Card(gameScreen, Art.Hex("FFFFFF"), new Vector2(980, 230), 8);
            shuffleBar = (RectTransform)sbInner.parent;
            // The shuffle bar takes the power bar's place while shuffling (see Relayout / SetShuffleBar), so it never covers any pot.
            shuffleInfo = UI.Txt(sbInner, "Select the containers to shuffle.", 36, Color.white); shuffleInfo.rectTransform.anchorMin = new Vector2(0, 0.42f); shuffleInfo.rectTransform.offsetMax = new Vector2(0, -6);
            var cancel = UI.Btn(sbInner, "Cancel", UI.Grey, new Vector2(300, 90), () => SetShuffleBar(false), null, 36);
            var cr = (RectTransform)cancel.transform; cr.anchorMin = new Vector2(0.03f, 0); cr.anchorMax = new Vector2(0.39f, 0); cr.offsetMin = new Vector2(0, 14); cr.offsetMax = new Vector2(0, 104);
            shuffleConfirmBtn = UI.Btn(sbInner, "Shuffle", UI.Purple, new Vector2(520, 90), ConfirmShuffle, Art.Coin, 36);
            var fr = (RectTransform)shuffleConfirmBtn.transform; fr.anchorMin = new Vector2(0.43f, 0); fr.anchorMax = new Vector2(0.97f, 0); fr.offsetMin = new Vector2(0, 14); fr.offsetMax = new Vector2(0, 104);
            shuffleBar.gameObject.SetActive(false);

            // tutorial hint bar
            var hInner = UI.Card(gameScreen, Art.Hex("FFF7C2"), new Vector2(980, 170), 6);
            hintBar = (RectTransform)hInner.parent;
            hintText = UI.Txt(hInner, "", 34, UI.Ink, TextAnchor.MiddleCenter, false); hintText.horizontalOverflow = HorizontalWrapMode.Wrap; hintText.rectTransform.offsetMin = new Vector2(20, 0); hintText.rectTransform.offsetMax = new Vector2(-20, 0);
            // Hints must never block taps: the pots underneath stay tappable, and the hint hides itself on the first tap (see TapPot) or after a few seconds.
            foreach (var g in hintBar.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            hintBar.gameObject.SetActive(false);
        }

        Button PowerBtn(int slot, string label, Color col, Sprite icon, Action onClick, string price, out Text priceText)
        {
            var b = UI.Btn(powerBar, "", col, new Vector2(210, 170), onClick, icon);
            var br = (RectTransform)b.transform; UI.Place(br, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(210, 170));
            var l = UI.Txt(powerBar, label, 36, UI.Ink, TextAnchor.MiddleCenter, false); UI.Place(l.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(240, 44));
            powerBtnRt[slot] = br; powerLabelRt[slot] = l.rectTransform;
            priceText = null;
            if (price != null)
            {
                var chip = UI.Card(powerBar, Art.Hex("FFFFFF"), new Vector2(170, 52), 5, false); var cr = (RectTransform)chip.parent;
                UI.Place(cr, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(170, 52));
                var coin = UI.Img(cr, "Coin", Art.Coin, Color.white); UI.Place(coin.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26, 0), new Vector2(46, 46));
                priceText = UI.Txt(cr, "900", 32, Color.white); priceText.rectTransform.offsetMin = new Vector2(44, 0);
                powerChipRt[slot] = cr;
            }
            return b;
        }

        // =====================================================================================  screens
        void ShowMenu()
        {
            menuScreen.gameObject.SetActive(true); gameScreen.gameObject.SetActive(false);
            backBtn.gameObject.SetActive(false); RefreshHud();
            RequestFocus(); PlayFade();
        }

        void ShowGame()
        {
            menuScreen.gameObject.SetActive(false); gameScreen.gameObject.SetActive(true);
            backBtn.gameObject.SetActive(true); RefreshHud();
            RequestFocus(); PlayFade();
        }

        public void RefreshHud()
        {
            if (goldText == null) return;
            goldText.text = Wallet.Balance.ToString("N0");
            livesText.text = Lives.Lives + "/" + Cfg.MaxLives;
            var t = Lives.TimeToNext; livesSub.text = Lives.Lives >= Cfg.MaxLives ? "" : string.Format("{0}:{1:00}", (int)t.TotalMinutes, t.Seconds);
            if (playLabel != null)
            {
                playLabel.text = HasResume ? Loc.F("Continue  -  Level {0}", D.resumeLevel) : Loc.F("Level {0}", PlayTargetLevel);
                progressText.text = Loc.F("{0} / {1} levels painted", D.stats.TotalCompleted, MaxLevel);
                if (dailyDot != null) dailyDot.gameObject.SetActive(Daily.CanClaimToday);
            }
            if (State != null && inGame)
            {
                var diff = CurrentDef.Difficulty;
                levelTitle.text = Loc.F("Level {0}", CurrentLevel); movesText.text = Loc.F("Moves {0}", State.MoveCount);
                diffText.text = Loc.T(Progression.Label(diff));
                diffBadge.color = diff == Difficulty.SuperHard ? UI.Red : diff == Difficulty.Hard ? UI.Orange : Art.Hex("35C6C5");
                ApplyPowerStates();
                UpdateShuffleBar();
            }
        }

        void TickLiveTexts()
        {
            for (int i = liveTexts.Count - 1; i >= 0; i--)
            {
                if (liveTexts[i].Key == null) { liveTexts.RemoveAt(i); continue; }
                liveTexts[i].Key.text = liveTexts[i].Value();
            }
        }

        static string Mmss(TimeSpan t) => string.Format("{0}:{1:00}", (int)t.TotalMinutes, t.Seconds);

        void SetShuffleBar(bool on)
        {
            shuffleMode = on;
            if (!on) { foreach (var i in shuffleMarks) Board.SetMarked(i, false); shuffleMarks.Clear(); }
            shuffleBar.gameObject.SetActive(on); powerBar.gameObject.SetActive(!on);
            if (on) { if (selected >= 0) { Board.SetSelected(selected, false); selected = -1; } }
            UpdateShuffleBar();
        }

        void UpdateShuffleBar()
        {
            if (!shuffleMode) return;
            shuffleInfo.text = shuffleMarks.Count < 2 ? "Select the containers to shuffle.\n(" + shuffleMarks.Count + " selected, pick at least 2)" : shuffleMarks.Count + " containers selected";
            UI.SetEnabled(shuffleConfirmBtn, shuffleMarks.Count >= 2);
            UI.SetLabel(shuffleConfirmBtn, D.freeShuffleTokens > 0 ? "Shuffle (FREE)" : "Shuffle  " + Cfg.ShufflePrice);
        }

        // =====================================================================================  toasts + hints
        public void Toast(string msg)
        {
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine(Loc.T(msg)));
        }

        IEnumerator ToastRoutine(string msg)
        {
            var cg = toastCard.GetComponent<CanvasGroup>();
            toastText.text = msg; cg.alpha = 1; toastCard.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(2.2f);
            for (float t = 0; t < 0.4f; t += Time.unscaledDeltaTime) { cg.alpha = 1 - t / 0.4f; yield return null; }
            toastCard.gameObject.SetActive(false);
        }

        void ShowHint(string text, float seconds = 7f)
        {
            if (hintRoutine != null) StopCoroutine(hintRoutine);
            hintText.text = Loc.T(text); hintBar.gameObject.SetActive(true);
            hintRoutine = StartCoroutine(Delay(seconds, () => hintBar.gameObject.SetActive(false)));
        }

        void ShowOnce(string id, string text)
        {
            if (D.seen.Contains(id)) return;
            D.seen.Add(id); ShowHint(text); Persist();
        }

        void ShowLevelHints(int id)
        {
            hintBar.gameObject.SetActive(false);
            switch (id)
            {
                case 1: ShowHint("Tap a pot to pick it up, then tap an empty pot to pour your paint into it!", 9); break;
                case 2: ShowHint("You can pour onto the same colour, or into an empty pot. Sort every colour into its own pot.", 9); break;
                case 3: ShowHint("All the matching colours on top move together in one pour.", 9); break;
                case 4: ShowHint("Each pot holds 4 layers. If it can't fit everything, only part of it pours.", 9); break;
                case 21: ShowHint("Dark ? layers hide a secret colour. Pour away what's on top to reveal it!", 10); break;
                case 31: ShowHint("Cloth-covered pots can't be used yet. Fill a pot with the colour shown on a cloth to uncover it!", 10); break;
                case 100: ShowHint("You unlocked the Leaderboard! Find it on the main menu.", 8); break;
            }
        }

        // =====================================================================================  popups
        public void CloseAllPopups() { foreach (var p in popups) if (p != null) Destroy(p); popups.Clear(); popupBack.Clear(); liveTexts.RemoveAll(k => k.Key == null); popupLayer.gameObject.SetActive(false); RequestFocus(); }

        public void ClosePopup(GameObject p) { popups.Remove(p); popupBack.Remove(p); if (p != null) Destroy(p); if (popups.Count == 0) popupLayer.gameObject.SetActive(false); RequestFocus(); }

        RectTransform Panel(string title, Vector2 size, out GameObject popupRoot, bool closable = true, Color? ribbon = null, Action onClose = null)
        {
            popupLayer.gameObject.SetActive(true);
            var proot = UI.Rect(popupLayer, "Popup"); UI.Stretch(proot); popupRoot = proot.gameObject; popups.Add(popupRoot);
            UI.Dim(proot, 0.6f);
            var inner = UI.Card(proot, Art.Hex("FFF7E6"), size, 10);
            var card = (RectTransform)inner.parent; UI.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            card.gameObject.AddComponent<CanvasGroup>(); card.gameObject.AddComponent<PopupCard>(); FitPopup(card);
            var ribbonCard = UI.Card(card, ribbon ?? UI.Orange, new Vector2(Mathf.Min(size.x - 100, 720), 110), 6, false);
            var rr = (RectTransform)ribbonCard.parent; UI.Place(rr, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -10), rr.sizeDelta);
            var tt = UI.Txt(ribbonCard, title, 56, Color.white); tt.resizeTextForBestFit = true; tt.resizeTextMinSize = 26; tt.resizeTextMaxSize = 56; tt.horizontalOverflow = HorizontalWrapMode.Wrap;
            var go = popupRoot;
            popupBack[go] = closable ? (Action)(() => { ClosePopup(go); onClose?.Invoke(); }) : null;     // Escape / Android Back / gamepad B
            if (closable)
            {
                var x = UI.Btn(card, "", UI.Red, new Vector2(84, 84), () => { ClosePopup(go); onClose?.Invoke(); }, Art.Plus);
                var xr = (RectTransform)x.transform; UI.Place(xr, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-20, -20), new Vector2(84, 84)); xr.localRotation = Quaternion.Euler(0, 0, 45);
            }
            var content = UI.Rect(inner, "Content"); UI.Stretch(content, 30, 30, 30, 90);
            var v = content.gameObject.AddComponent<VerticalLayoutGroup>(); v.spacing = 18; v.childAlignment = TextAnchor.UpperCenter; v.childControlWidth = false; v.childControlHeight = true; v.childForceExpandWidth = false; v.childForceExpandHeight = false;
            v.padding = new RectOffset(0, 0, 10, 0);
            RequestFocus();
            return content;
        }

        static RectTransform Item(Transform parent, float w, float h)
        {
            var r = UI.Rect(parent, "Item"); r.sizeDelta = new Vector2(w, h);
            var le = r.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = h; le.preferredWidth = w; le.minHeight = h;
            return r;
        }

        Text Line(Transform parent, string text, int size, Color col, float w, float h, TextAnchor a = TextAnchor.MiddleCenter, bool outline = false)
        {
            var it = Item(parent, w, h); var t = UI.Txt(it, text, size, col, a, outline); t.horizontalOverflow = HorizontalWrapMode.Wrap; return t;
        }

        Button PopBtn(Transform parent, string label, Color col, float w, float h, Action act, GameObject closeFirst = null, Sprite icon = null, int fs = 42)
        {
            var it = Item(parent, w, h);
            var b = UI.Btn(it, label, col, new Vector2(w, h), () => { if (closeFirst != null) ClosePopup(closeFirst); act?.Invoke(); }, icon, fs);
            var r = (RectTransform)b.transform; r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f); r.anchoredPosition = Vector2.zero;
            return b;
        }

        RectTransform BtnRow(Transform parent, float w, float h)
        {
            var it = Item(parent, w, h); var hg = it.gameObject.AddComponent<HorizontalLayoutGroup>();
            hg.spacing = 20; hg.childAlignment = TextAnchor.MiddleCenter; hg.childControlWidth = false; hg.childControlHeight = false; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            return it;
        }

        Button RowBtn(RectTransform row, string label, Color col, float w, float h, Action act, GameObject closeFirst, int fs = 42)
        {
            var b = UI.Btn(row, label, col, new Vector2(w, h), () => { if (closeFirst != null) ClosePopup(closeFirst); act?.Invoke(); }, null, fs);
            return b;
        }

        public void OpenDialog(string title, string body, params (string label, Color col, Action act)[] buttons)
        {
            var c = Panel(title, new Vector2(900, 620), out var go, false);
            Line(c, body, 42, UI.Ink, 800, 250);
            var row = BtnRow(c, 840, 130); float w = (840 - 20 * (buttons.Length - 1)) / buttons.Length;
            foreach (var b in buttons) RowBtn(row, b.label, b.col, w, 120, b.act, go);
        }

        public void OpenLeaveConfirm()
        {
            int aid = AttemptId;
            OpenDialog("Leave this level?", "This attempt will use 1 life.", ("Keep Playing", UI.Green, null), ("Leave Level", UI.Red, () => ConsumeLifeAndLeave(aid)));
        }

        void OpenRescue()
        {
            var c = Panel("No more moves!", new Vector2(940, 1120), out var go, false, UI.Red);
            Line(c, "The board is stuck. Pick a helper to keep painting.", 40, UI.Ink, 840, 110);
            // only offer helpers the player has already been introduced to
            if (State.CanUndo && IsUnlocked(PowerUp.Undo)) PopBtn(c, "Undo (free)", UI.Blue, 800, 120, OnUndo, go, Art.Undo);
            if (IsUnlocked(PowerUp.ExtraPot))
            {
                bool potOk = D.freeExtraPotTokens > 0 || Wallet.CanAfford(Cfg.ExtraContainerPrice);
                if (potOk) PopBtn(c, "Add Container   " + Cfg.ExtraContainerPrice, UI.Green, 800, 120, OnExtraPotButton, go, Art.Plus);
                else AdOrDisabled(c, go, "Free Container (ad)", () => { D.freeExtraPotTokens++; ConfirmAddPot(true); });
            }
            if (IsUnlocked(PowerUp.Shuffle))
            {
                bool shOk = D.freeShuffleTokens > 0 || Wallet.CanAfford(Cfg.ShufflePrice);
                if (shOk) PopBtn(c, "Targeted Shuffle   " + Cfg.ShufflePrice, UI.Purple, 800, 120, OnShuffleButton, go, Art.Shuffle);
                else AdOrDisabled(c, go, "Free Shuffle (ad)", () => { D.freeShuffleTokens++; Persist(); OnShuffleButton(); });
            }
            if (Cfg.ShopEnabled) PopBtn(c, "Get Gold", UI.Orange, 800, 110, OpenShop, go, Art.Coin);
            PopBtn(c, "Not now", UI.Grey, 800, 110, OpenLeaveConfirm, go);
        }

        void AdOrDisabled(Transform c, GameObject go, string label, Action reward)
        {
            var b = PopBtn(c, "Watch Ad: " + label, UI.Green, 800, 120, () => WatchRewarded("rescue", reward), go, null, 38);
            if (!AdPol.CooldownReady) { UI.SetEnabled(b, false); var t = b.GetComponentInChildren<Text>(); liveTexts.Add(new KeyValuePair<Text, Func<string>>(t, () => AdPol.CooldownReady ? "Watch Ad: " + label : "Ad ready in " + Mmss(TimeSpan.FromSeconds(AdPol.SecondsLeft)))); }
        }

        public void OpenInsufficient(string what, int price, Action adReward)
        {
            var c = Panel("Not enough Gold", new Vector2(920, 840), out var go, true, UI.Red);
            Line(c, what + " costs " + price + " Gold.\nYou have " + Wallet.Balance + ".", 44, UI.Ink, 820, 190);
            if (adReward != null) AdOrDisabled(c, go, "Free use", adReward);
            if (Cfg.ShopEnabled) PopBtn(c, "Get Gold", UI.Orange, 800, 120, OpenShop, go, Art.Coin);
            PopBtn(c, "Cancel", UI.Grey, 800, 110, null, go);
        }

        void OpenConfirmPurchase(string name, string desc, int price, Action confirm)
        {
            var c = Panel(name, new Vector2(920, 840), out var go, true);
            Line(c, desc, 40, UI.Ink, 820, 150);
            var row = Item(c, 800, 90); var coin = UI.Img(row, "Coin", Art.Coin, Color.white); UI.Place(coin.rectTransform, new Vector2(0.35f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 80));
            var pt = UI.Txt(row, price.ToString("N0"), 64, Color.white); pt.rectTransform.anchorMin = new Vector2(0.45f, 0); pt.alignment = TextAnchor.MiddleLeft;
            Line(c, "Your Gold: " + Wallet.Balance.ToString("N0"), 38, UI.Ink, 800, 60);
            var br = BtnRow(c, 840, 130);
            RowBtn(br, "Cancel", UI.Grey, 400, 120, null, go); RowBtn(br, "Confirm", UI.Green, 400, 120, confirm, go);
        }

        public void OpenNoLives()
        {
            var c = Panel("Out of lives!", new Vector2(920, 900), out var go, true, UI.Red);
            var heart = Item(c, 200, 180); var h = UI.Img(heart, "H", Art.Heart, new Color(0.7f, 0.7f, 0.75f, 1f)); UI.Stretch(h.rectTransform);
            Line(c, "Lives refill one every " + Cfg.MinutesPerLife + " minutes.", 38, UI.Ink, 820, 70);
            var t = Line(c, "", 64, UI.Red, 820, 100, TextAnchor.MiddleCenter, true);
            liveTexts.Add(new KeyValuePair<Text, Func<string>>(t, () => Lives.Lives > 0 ? "A life is ready!" : "Next life in " + Mmss(Lives.TimeToNext)));
            t.text = "Next life in " + Mmss(Lives.TimeToNext);
            if (Cfg.RewardedLifeEnabled) AdOrDisabled(c, go, "+1 Life", () => { Lives.Add(Cfg.AdLifeReward); Persist(); Toast("+1 life!"); });
            PopBtn(c, "OK", UI.Grey, 800, 110, null, go);
        }

        public void OpenLevelComplete(int level, int basePay, int bonus, List<AchievementDef> newAch)
        {
            var c = Panel("Level " + level + " Complete!", new Vector2(960, 1380), out var go, false, UI.Green);
            var star = Item(c, 220, 190); var s = UI.Img(star, "S", Art.StarIcon, Color.white); UI.Stretch(s.rectTransform);
            int total = basePay + bonus;
            if (total > 0)
            {
                Line(c, "Level reward   +" + basePay, 46, UI.Ink, 860, 64);
                if (bonus > 0) Line(c, "Milestone bonus   +" + bonus, 46, UI.Orange, 860, 64);
                var row = Item(c, 800, 120); var coin = UI.Img(row, "Coin", Art.Coin, Color.white); UI.Place(coin.rectTransform, new Vector2(0.3f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100));
                var tt = UI.Txt(row, "+" + total, 90, Color.white); tt.rectTransform.anchorMin = new Vector2(0.4f, 0); tt.alignment = TextAnchor.MiddleLeft;
                Fx.Burst(popupLayer, new Vector2(0, 80), UI.Gold, 14, 500, 34);
                Audio.Play("coin");
            }
            else Line(c, "Already painted before — no new reward.", 40, UI.Ink, 860, 80);
            foreach (var a in newAch) Line(c, "Achievement: " + a.Title + (a.Reward > 0 ? "  +" + a.Reward : ""), 36, UI.Purple, 860, 56, TextAnchor.MiddleCenter, false);
            if (total > 0)
            {
                Button dbl = null; bool done = false;
                dbl = PopBtn(c, "Watch Ad: 2x Gold", UI.Green, 820, 120, () =>
                {
                    if (done) return;
                    WatchRewarded("double_gold", () =>
                    {
                        if (done) return; done = true;
                        if (Wallet.Grant(total, "double_gold", level, "double_" + level)) { Toast("Doubled! +" + total + " more Gold"); Audio.Play("coin"); Fx.Burst(popupLayer, new Vector2(0, 80), UI.Gold, 14, 500, 34); }
                        UI.SetEnabled(dbl, false); UI.SetLabel(dbl, "Doubled!");
                    });
                }, null, Art.Coin, 40);
                if (!AdPol.CooldownReady) liveTexts.Add(new KeyValuePair<Text, Func<string>>(dbl.GetComponentInChildren<Text>(), () => done ? "Doubled!" : AdPol.CooldownReady ? "Watch Ad: 2x Gold" : "2x ready in " + Mmss(TimeSpan.FromSeconds(AdPol.SecondsLeft))));
            }
            var br = BtnRow(c, 860, 130);
            RowBtn(br, "Menu", UI.Grey, 260, 120, LeaveToMenu, go);
            RowBtn(br, level < MaxLevel ? "Next Level" : "Done", UI.Blue, 560, 120, () => { if (level < MaxLevel) StartLevel(level + 1); else LeaveToMenu(); }, go, 46);
        }

        // ---------------------------------------------------------------- daily rewards
        public void OpenDaily()
        {
            var c = Panel("Daily Rewards", new Vector2(980, 1120), out var go, true, UI.Pink);
            Line(c, "Claim all 5 rewards this week for a big bonus. Missed a day? You have 2 spare days!", 34, UI.Ink, 880, 110);
            var row = Item(c, 900, 270); var hg = row.gameObject.AddComponent<HorizontalLayoutGroup>(); hg.spacing = 14; hg.childAlignment = TextAnchor.MiddleCenter; hg.childControlWidth = false; hg.childControlHeight = false; hg.childForceExpandHeight = false; hg.childForceExpandWidth = false;
            for (int i = 0; i < Cfg.DailyRewards.Length; i++)
            {
                var st = Daily.TileState(i);
                var face = st == DailyTileState.Claimed ? Art.Hex("C9F3D6") : st == DailyTileState.Available ? Art.Hex("FFE08A") : Art.Hex("FFFFFF");
                var inner = UI.Card(row, face, new Vector2(168, 250), 6); var tr = (RectTransform)inner.parent;
                var le = tr.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = 168; le.preferredHeight = 250;
                var d = UI.Txt(inner, "Day " + (i + 1), 32, Color.white); d.rectTransform.anchorMin = new Vector2(0, 0.8f);
                var ic = UI.Img(inner, "I", Art.Coin, Color.white); UI.Place(ic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(96, 96));
                var am = UI.Txt(inner, Cfg.DailyRewards[i].ToString(), 42, Color.white); am.rectTransform.anchorMax = new Vector2(1, 0.3f);
                if (st == DailyTileState.Claimed) { var ck = UI.Img(inner, "Ck", Art.Check, Art.Hex("35B86B")); UI.Place(ck.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(120, 120)); }
            }
            var bonus = UI.Card(c, Daily.BonusPaid ? Art.Hex("C9F3D6") : Art.Hex("FFFFFF"), new Vector2(880, 110), 6); var br = (RectTransform)bonus.parent;
            var ble = br.gameObject.AddComponent<LayoutElement>(); ble.preferredHeight = 110; ble.preferredWidth = 880;
            UI.Txt(bonus, "Full week bonus:  +" + Cfg.WeeklyBonus.ToString("N0") + " Gold", 42, Color.white);
            Line(c, Daily.ClaimedCount + " / " + Cfg.DailyRewards.Length + " claimed this week", 40, UI.Ink, 880, 60);
            var timer = Line(c, "", 34, UI.Ink, 880, 50);
            liveTexts.Add(new KeyValuePair<Text, Func<string>>(timer, () => "Week resets in " + (int)Daily.TimeToReset.TotalHours + "h " + Daily.TimeToReset.Minutes + "m"));
            timer.text = "Week resets in " + (int)Daily.TimeToReset.TotalHours + "h " + Daily.TimeToReset.Minutes + "m";
            if (Daily.CanClaimToday) PopBtn(c, "Claim " + Cfg.DailyRewards[Daily.ClaimedCount], UI.Green, 700, 130, () => { ClaimDaily(); OpenDaily(); }, go, Art.Coin, 50);
            else PopBtn(c, Daily.ClaimedCount >= Cfg.DailyRewards.Length ? "All claimed!" : "Come back tomorrow", UI.Grey, 700, 120, null, null);
        }

        // ---------------------------------------------------------------- achievements
        void OpenAchievements()
        {
            var c = Panel("Achievements", new Vector2(980, 1560), out var go, true, UI.Purple);
            var sv = UI.Rect(c, "Scroll"); var le = sv.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 1330; le.preferredWidth = 900; sv.sizeDelta = new Vector2(900, 1330);
            var sr = sv.gameObject.AddComponent<ScrollRect>(); sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Elastic; sr.scrollSensitivity = 40;
            var vp = UI.Rect(sv, "Viewport"); UI.Stretch(vp); vp.gameObject.AddComponent<RectMask2D>(); var vpi = vp.gameObject.AddComponent<Image>(); vpi.color = new Color(0, 0, 0, 0.001f);
            var content = UI.Rect(vp, "Content"); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1); content.sizeDelta = Vector2.zero;
            var vl = content.gameObject.AddComponent<VerticalLayoutGroup>(); vl.spacing = 14; vl.childControlHeight = true; vl.childControlWidth = false; vl.childForceExpandHeight = false; vl.childForceExpandWidth = false; vl.childAlignment = TextAnchor.UpperCenter;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = vp; sr.content = content;
            foreach (var a in Achievements.All)
            {
                bool got = D.achievements.Contains(a.Id);
                var inner = UI.Card(content, got ? Art.Hex("D8F7E2") : Art.Hex("FFFFFF"), new Vector2(880, 130), 5, false); var rr = (RectTransform)inner.parent;
                var rle = rr.gameObject.AddComponent<LayoutElement>(); rle.preferredHeight = 130; rle.preferredWidth = 880;
                var ic = UI.Img(inner, "I", got ? Art.Medal : Art.Lock, got ? Color.white : new Color(1, 1, 1, 0.6f)); UI.Place(ic.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(70, 0), new Vector2(96, 96));
                var tt = UI.Txt(inner, a.Title, 38, Color.white, TextAnchor.MiddleLeft); tt.rectTransform.anchorMin = new Vector2(0, 0.5f); tt.rectTransform.offsetMin = new Vector2(140, 0); tt.rectTransform.offsetMax = new Vector2(-190, 0);
                var dd = UI.Txt(inner, a.Description, 28, UI.Ink, TextAnchor.MiddleLeft, false); dd.rectTransform.anchorMax = new Vector2(1, 0.5f); dd.rectTransform.offsetMin = new Vector2(140, 0); dd.rectTransform.offsetMax = new Vector2(-190, 0);
                var rw = UI.Txt(inner, got ? "Done!" : (a.Reward > 0 ? "+" + a.Reward : "Badge"), 36, got ? Art.Hex("35B86B") : UI.Orange, TextAnchor.MiddleRight); rw.rectTransform.offsetMax = new Vector2(-18, 0);
            }
        }

        // ---------------------------------------------------------------- leaderboard
        public void OpenLeaderboard()
        {
            var c = Panel("Leaderboard", new Vector2(960, 1300), out var go, true, UI.Orange);
            if (D.stats.HighestCompleted < Cfg.LeaderboardUnlockLevel)
            {
                var lk = Item(c, 220, 220); var l = UI.Img(lk, "L", Art.Lock, Color.white); UI.Stretch(l.rectTransform);
                Line(c, "Reach Level " + Cfg.LeaderboardUnlockLevel + " to unlock the leaderboard!", 48, UI.Ink, 820, 150);
                Line(c, "Levels completed so far: " + D.stats.HighestCompleted + " / " + Cfg.LeaderboardUnlockLevel, 38, UI.Ink, 820, 80);
                return;
            }
            Line(c, Leaderboard.Connected ? "Ranked by highest level, then total levels." : "Local test leaderboard (no online service connected).", 32, UI.Ink, 860, 80);
            int rank = 1;
            foreach (var e in Leaderboard.GetEntries(D.stats.HighestCompleted, D.stats.TotalCompleted))
            {
                var inner = UI.Card(c, e.IsYou ? Art.Hex("FFE08A") : Art.Hex("FFFFFF"), new Vector2(860, 96), 5, false); var rr = (RectTransform)inner.parent;
                var le = rr.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 96; le.preferredWidth = 860;
                var rk = UI.Txt(inner, "#" + rank, 40, Color.white, TextAnchor.MiddleLeft); rk.rectTransform.offsetMin = new Vector2(24, 0);
                var nm = UI.Txt(inner, e.Name, 38, Color.white, TextAnchor.MiddleLeft); nm.rectTransform.offsetMin = new Vector2(150, 0);
                var sc = UI.Txt(inner, "Lv " + e.HighestLevel + "  (" + e.TotalLevels + ")", 36, UI.Ink, TextAnchor.MiddleRight, false); sc.rectTransform.offsetMax = new Vector2(-24, 0);
                rank++;
            }
        }

        // ---------------------------------------------------------------- shop (scaffold)
        public void OpenShop()
        {
            Analytics.Event("shop_opened");
            var c = Panel("Gold Shop", new Vector2(980, 1300), out var go, true, UI.Blue);
            if (!Cfg.ShopEnabled) Line(c, "Coming soon! Real-money purchases are switched off until store billing is set up.", 36, UI.Red, 880, 120);
            else Line(c, "Prices come from the store catalogue.", 34, UI.Ink, 880, 60);
            foreach (var p in Purchases.Products)
            {
                var pack = p;
                var inner = UI.Card(c, Art.Hex("FFFFFF"), new Vector2(880, 130), 5, false); var rr = (RectTransform)inner.parent;
                var le = rr.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 130; le.preferredWidth = 880;
                var ic = UI.Img(inner, "I", Art.Coin, Color.white); UI.Place(ic.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(76, 0), new Vector2(96, 96));
                var tt = UI.Txt(inner, pack.Title, 36, Color.white, TextAnchor.MiddleLeft); tt.rectTransform.anchorMin = new Vector2(0, 0.45f); tt.rectTransform.offsetMin = new Vector2(150, 0);
                var gg = UI.Txt(inner, pack.Gold.ToString("N0") + " Gold", 30, UI.Ink, TextAnchor.MiddleLeft, false); gg.rectTransform.anchorMax = new Vector2(1, 0.45f); gg.rectTransform.offsetMin = new Vector2(150, 0);
                var buy = UI.Btn(inner, Cfg.ShopEnabled ? pack.PlaceholderPrice : "Soon", UI.Green, new Vector2(220, 90), () => BuyPack(pack), null, 36);
                UI.Place((RectTransform)buy.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(220, 90));
                UI.SetEnabled(buy, Cfg.ShopEnabled && Purchases.Available);
            }
        }

        void BuyPack(GoldPack p)
        {
            Analytics.Event("purchase_started", p.ProductId);
            Purchases.Purchase(p, (ok, token) =>
            {
                if (!ok) { Toast(token); return; }
                // Gold only ever comes from a verified purchase result; the token makes the grant idempotent.
                if (Wallet.Grant(p.Gold, "iap_purchase", 0, "iap_" + token)) { Analytics.Event("purchase_completed", p.ProductId); Toast("+" + p.Gold + " Gold"); Audio.Play("coin"); }
            });
        }

        // ---------------------------------------------------------------- settings
        void OpenSettings()
        {
            var c = Panel("Settings", new Vector2(940, 1380), out var go, true, UI.Blue);
            Toggle(c, "Music", Audio.MusicOn, v => { Audio.MusicOn = v; Persist(); });
            Toggle(c, "Sound effects", Audio.SfxOn, v => { Audio.SfxOn = v; Persist(); });
            Toggle(c, "Haptics", Audio.HapticsOn, v => { Audio.HapticsOn = v; Persist(); });
            Toggle(c, "Colour-blind symbols", D.colorAssist, v => { D.colorAssist = v; Persist(); if (Board != null && inGame) Board.RenderAll(); });
            Toggle(c, "Reduced motion", D.reducedMotion, v => { D.reducedMotion = v; Persist(); });
            PopBtn(c, Loc.NameOf(Loc.Language), UI.Purple, 820, 100, () => { Loc.SetLanguage(Loc.NextLanguage()); Persist(); Toast(Loc.T("Language changes fully the next time you open the game.")); OpenSettings(); }, go, null, 40);
            var row = BtnRow(c, 860, 110);
            RowBtn(row, "Privacy", UI.Grey, 410, 100, () => Toast("Consent options will appear once an ad SDK is connected."), null, 36);
            RowBtn(row, "Restore Buys", UI.Grey, 410, 100, () => Purchases.Restore(m => Toast(m)), null, 34);
            Line(c, Application.productName + "  v" + Application.version, 28, UI.Ink, 820, 40);
            PopBtn(c, "Reset Progress", UI.Red, 820, 110, () => OpenDialog("Reset everything?", "This erases levels, Gold, lives and achievements. It cannot be undone.", ("Cancel", UI.Grey, null), ("Erase", UI.Red, () => OpenDialog("Really erase?", "Last chance — all progress will be deleted.", ("Keep it", UI.Green, null), ("Erase now", UI.Red, ResetAllProgress)))), go);
        }

        void Toggle(Transform parent, string label, bool initial, Action<bool> set)
        {
            var inner = UI.Card(parent, Art.Hex("FFFFFF"), new Vector2(840, 100), 5, false); var rr = (RectTransform)inner.parent;
            var le = rr.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 100; le.preferredWidth = 840;
            var t = UI.Txt(inner, label, 40, Color.white, TextAnchor.MiddleLeft); t.rectTransform.offsetMin = new Vector2(30, 0);
            bool val = initial;
            var b = UI.Btn(inner, val ? "ON" : "OFF", val ? UI.Green : UI.Grey, new Vector2(190, 76), null, null, 36);
            UI.Place((RectTransform)b.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(190, 76));
            b.onClick.AddListener(() =>
            {
                val = !val; set(val); UI.SetLabel(b, val ? "ON" : "OFF");
                var face = b.transform.Find("Face").GetComponent<Image>(); face.color = val ? UI.Green : UI.Grey; var m = b.GetComponent<ButtonMemo>() ?? b.gameObject.AddComponent<ButtonMemo>(); m.face = face.color;
            });
        }

        // ---------------------------------------------------------------- fake rewarded ad (development stand-in for a real ad SDK)
        Action ShowFakeAd(Action<AdResult> cb)
        {
            var c = Panel("Test Ad", new Vector2(900, 700), out var go, false, UI.Grey);
            Line(c, "This is a TEST rewarded ad.\nA real ad SDK replaces this screen.", 38, UI.Ink, 800, 150);
            var t = Line(c, "", 56, UI.Purple, 800, 90, TextAnchor.MiddleCenter, true);
            float start = Time.unscaledTime; bool finished = false; Button claim = null; Button close = null;
            var row = BtnRow(c, 840, 130);
            close = RowBtn(row, "Close early", UI.Grey, 400, 120, () => { if (finished) return; finished = true; ClosePopup(go); cb(AdResult.ClosedEarly); }, null, 36);
            claim = RowBtn(row, "Get Reward", UI.Green, 400, 120, () => { if (finished) return; finished = true; ClosePopup(go); cb(AdResult.Completed); }, null, 36);
            UI.SetEnabled(claim, false);
            liveTexts.Add(new KeyValuePair<Text, Func<string>>(t, () =>
            {
                float left = 3f - (Time.unscaledTime - start);
                if (left <= 0 && claim != null && !finished) { UI.SetEnabled(claim, true); return "Reward ready!"; }
                return left > 0 ? "Ad ends in " + Mathf.CeilToInt(left) : "Reward ready!";
            }));
            StartCoroutine(TickFor(3.3f));
            return () => { if (!finished) { finished = true; ClosePopup(go); cb(AdResult.Failed); } };
        }

        IEnumerator TickFor(float seconds)
        {
            for (float t = 0; t < seconds; t += 0.2f) { TickLiveTexts(); yield return new WaitForSecondsRealtime(0.2f); }
            TickLiveTexts();
        }
    }

    /// <summary>Pop-in animation for popup cards. <see cref="Fit"/> is the scale that makes the card fit the screen; the animation multiplies it.</summary>
    public sealed class PopupCard : MonoBehaviour
    {
        const float Duration = 0.18f;
        public float Fit = 1f;
        float t; CanvasGroup group;

        void Awake() { group = GetComponent<CanvasGroup>(); Apply(); }

        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / Duration);
            Apply();
        }

        public void Apply()
        {
            float k = t;
            float c1 = 1.70158f, c3 = c1 + 1f, x = k - 1f;
            float easeOutBack = 1f + c3 * x * x * x + c1 * x * x;          // overshoots slightly, then settles
            transform.localScale = Vector3.one * (Fit * (0.8f + 0.2f * easeOutBack));
            if (group != null) group.alpha = Mathf.Clamp01(k * 1.6f);
        }
    }
}
