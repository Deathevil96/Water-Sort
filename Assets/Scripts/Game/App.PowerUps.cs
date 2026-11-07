using UnityEngine;
using UnityEngine.UI;

namespace PaintSort.Game
{
    /// <summary>
    /// Power-up progression. Undo, Shuffle and the extra pot unlock at levels 5, 10 and 15. Each is introduced by a popup the first time
    /// its level starts, the two paid ones come with a free token to try, and the new button pulses until it is used.
    /// Levels before 5 are designed so no power-up is ever needed (see DeadEndAnalysis).
    /// </summary>
    public sealed partial class App
    {
        static readonly PowerUp[] AllPowerUps = { PowerUp.Undo, PowerUp.Shuffle, PowerUp.ExtraPot };
        GameObject powerHighlight; PowerUp? highlighted;

        Button ButtonOf(PowerUp p) => p == PowerUp.Undo ? undoBtn : p == PowerUp.Shuffle ? shuffleBtn : potBtn;

        /// <summary>Greys and locks the buttons the player has not unlocked yet; locked paid buttons show the level they unlock at instead of a price.</summary>
        void ApplyPowerStates()
        {
            bool undoOpen = IsUnlocked(PowerUp.Undo), shuffleOpen = IsUnlocked(PowerUp.Shuffle), potOpen = IsUnlocked(PowerUp.ExtraPot);
            UI.SetLocked(undoBtn, !undoOpen);
            UI.SetLocked(shuffleBtn, !shuffleOpen);
            UI.SetLocked(potBtn, !potOpen);
            if (undoOpen) UI.SetEnabled(undoBtn, State.CanUndo);        // locked buttons stay clickable so a tap can say when they unlock
            shufflePrice.text = !shuffleOpen ? Loc.F("Lv {0}", UnlockLevel(PowerUp.Shuffle)) : D.freeShuffleTokens > 0 ? Loc.T("FREE") : Cfg.ShufflePrice.ToString("N0");
            potPrice.text = !potOpen ? Loc.F("Lv {0}", UnlockLevel(PowerUp.ExtraPot)) : D.freeExtraPotTokens > 0 ? Loc.T("FREE") : Cfg.ExtraContainerPrice.ToString("N0");
        }

        /// <summary>On the first start of a power-up's unlock level: grant its free token (paid ones) and open the intro popup.</summary>
        void MaybeIntroducePowerUp(int levelId)
        {
            foreach (var p in AllPowerUps)
            {
                string key = "powerup_" + p;
                if (UnlockLevel(p) != levelId || D.seen.Contains(key)) continue;
                D.seen.Add(key);
                if (p == PowerUp.Shuffle) D.freeShuffleTokens++;
                else if (p == PowerUp.ExtraPot) D.freeExtraPotTokens++;
                Persist();
                int attempt = AttemptId;
                StartCoroutine(Delay(0.5f, () => { if (inGame && attempt == AttemptId && !PopupOpen) OpenPowerUpIntro(p); }));
                return;
            }
        }

        string IntroText(PowerUp p)
        {
            switch (p)
            {
                case PowerUp.Undo: return "Made a mistake? Undo takes back your last pour. It is always free, so use it as much as you like!";
                case PowerUp.Shuffle: return "Stuck? Pick two or more pots and Shuffle mixes their paint into a fresh arrangement that can still be finished.";
                default: return "Out of room? + Pot adds one extra empty pot until this level ends.";
            }
        }

        void OpenPowerUpIntro(PowerUp p)
        {
            Sprite icon = p == PowerUp.Undo ? Art.Undo : p == PowerUp.Shuffle ? Art.Shuffle : Art.Plus;
            Color tint = p == PowerUp.Undo ? UI.Blue : p == PowerUp.Shuffle ? UI.Purple : UI.Green;
            string name = p == PowerUp.Undo ? "Undo" : p == PowerUp.Shuffle ? "Shuffle" : "Extra pot";
            var c = Panel(Loc.F("New power-up: {0}!", Loc.T(name)), new Vector2(940, 880), out var go, false, tint);

            var iconHolder = Item(c, 240, 200);
            var inner = UI.Card(iconHolder, tint, new Vector2(240, 200), 8);
            var card = (RectTransform)inner.parent; UI.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 200));
            var ic = UI.Img(inner, "Icon", icon, Color.white); UI.Stretch(ic.rectTransform, 24, 20, 24, 20);

            Line(c, IntroText(p), 40, UI.Ink, 840, 250);
            if (p == PowerUp.Shuffle) Line(c, Loc.F("Costs {0} Gold. Here is a FREE one to try!", Cfg.ShufflePrice), 38, UI.Orange, 840, 120, TextAnchor.MiddleCenter, false);
            else if (p == PowerUp.ExtraPot) Line(c, Loc.F("Costs {0} Gold. Here is a FREE one to try!", Cfg.ExtraContainerPrice), 38, UI.Orange, 840, 120, TextAnchor.MiddleCenter, false);
            PopBtn(c, "Got it!", UI.Green, 700, 130, () => HighlightPower(p), go, null, 52);
            Audio.Play("reveal");
        }

        void HighlightPower(PowerUp p)
        {
            ClearPowerHighlight();
            var button = ButtonOf(p);
            var ringRt = UI.Rect(button.transform, "Hint");
            UI.Stretch(ringRt, -20, -20, -20, -20);
            var img = ringRt.gameObject.AddComponent<Image>(); img.sprite = Art.Round; img.type = Image.Type.Sliced; img.color = UI.FocusRing; img.raycastTarget = false;
            ringRt.SetAsFirstSibling();
            UI.SubCanvas(ringRt, false);                      // own canvas: the pulse never rebuilds the rest of the screen
            ringRt.gameObject.AddComponent<PulseRing>();
            powerHighlight = ringRt.gameObject; highlighted = p;
        }

        /// <summary>Removes the pulse (called when the highlighted power-up is used, or the level changes).</summary>
        void ClearPowerHighlight() { if (powerHighlight != null) Destroy(powerHighlight); powerHighlight = null; highlighted = null; }

        void OnPowerUsed(PowerUp p) { if (highlighted == p) ClearPowerHighlight(); }
    }
}
