using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PaintSort.Game
{
    /// <summary>
    /// Non-touch input: Escape / Android Back / gamepad B, and keyboard / gamepad / TV-remote navigation.
    /// Touch and mouse go through the normal uGUI event system. A focus ring is only drawn once a non-pointer device has been used.
    /// On the board a cursor moves between pots; buttons use uGUI's built-in navigation, and the two hand focus to each other at the edges.
    /// </summary>
    public sealed partial class App
    {
        const float EdgeAlongBias = 2.2f;                   // how strongly "in line with the direction pressed" beats "closest"

        Dictionary<GameObject, Action> popupBack;           // popup -> action for Back (null when the popup cannot be dismissed)
        int cursor;                                         // board cursor (pot index) for keyboard / gamepad / remote
        bool focusRequested;
        Button settingsBtn;

        /// <summary>Asks for the focus to be re-established on the next frame (after a screen or popup change, once the UI is built).</summary>
        void RequestFocus() { focusRequested = true; }

        void PollInput()
        {
            var kb = Keyboard.current; var pad = Gamepad.current;
            if (PointerPressed())
            {
                if (InputMode.FocusVisible) { InputMode.FocusVisible = false; if (Board != null) Board.SetFocus(-1); }
                focusRequested = false;
                return;
            }

            bool cancel = (kb != null && kb.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame);
            var dir = ReadDirection(kb, pad);
            bool submit = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) || (pad != null && pad.buttonSouth.wasPressedThisFrame);

            if (cancel) { HandleBack(); return; }
            bool navigated = dir != Vector2.zero || submit;
            if (navigated && !InputMode.FocusVisible) { InputMode.FocusVisible = true; focusRequested = true; }   // the first press only reveals the focus
            else if (navigated) HandleNavigation(dir, submit);

            if (focusRequested) ApplyFocus();
        }

        static bool PointerPressed()
        {
            var mouse = Mouse.current; var touch = Touchscreen.current;
            return (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
                || (touch != null && touch.primaryTouch.press.wasPressedThisFrame);
        }

        static Vector2 ReadDirection(Keyboard kb, Gamepad pad)
        {
            Vector2 d = Vector2.zero;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) d.x = -1;
                else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) d.x = 1;
                else if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) d.y = 1;
                else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) d.y = -1;
            }
            if (d == Vector2.zero && pad != null)
            {
                if (pad.dpad.left.wasPressedThisFrame) d.x = -1;
                else if (pad.dpad.right.wasPressedThisFrame) d.x = 1;
                else if (pad.dpad.up.wasPressedThisFrame) d.y = 1;
                else if (pad.dpad.down.wasPressedThisFrame) d.y = -1;
            }
            return d;
        }

        void HandleBack()
        {
            if (PopupOpen)
            {
                var top = popups[popups.Count - 1];
                if (popupBack.TryGetValue(top, out var act) && act != null) act();
                return;
            }
            if (shuffleMode) { SetShuffleBar(false); return; }
            if (inGame) OnBackButton();
        }

        bool BoardHasFocus => inGame && !PopupOpen && Board != null && Board.Pots.Count > 0
                              && (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null);

        void HandleNavigation(Vector2 dir, bool submit)
        {
            if (BoardHasFocus)
            {
                if (submit) { TapPot(cursor); return; }
                var next = Board.NearestPot(cursor, dir);
                if (next != null) { cursor = next.Index; Audio.Play("tap"); Board.SetFocus(cursor); }
                else LeaveBoard(dir);
                return;
            }

            // a button is focused: uGUI moves between buttons; when there is nothing further in that direction, return to the board
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (inGame && !PopupOpen && selected != null && dir != Vector2.zero)
            {
                var sel = selected.GetComponent<Selectable>();
                Selectable neighbour = sel == null ? null
                    : dir.y > 0 ? sel.FindSelectableOnUp() : dir.y < 0 ? sel.FindSelectableOnDown() : dir.x < 0 ? sel.FindSelectableOnLeft() : sel.FindSelectableOnRight();
                if (neighbour == null) ApplyFocus();
            }
        }

        /// <summary>The cursor ran out of pots in a direction: hand focus to the closest HUD button that way (undo / shuffle / pot / restart / back / settings).</summary>
        void LeaveBoard(Vector2 dir)
        {
            Button best = null; float bestScore = float.MaxValue;
            Vector2 origin = Board.Pots[Mathf.Clamp(cursor, 0, Board.Pots.Count - 1)].Rt.position;
            foreach (var b in HudButtons())
            {
                if (b == null || !b.gameObject.activeInHierarchy || !b.interactable) continue;
                Vector2 d = (Vector2)b.transform.position - origin;
                float along = Vector2.Dot(d, dir); if (along <= 1f) continue;
                float across = Mathf.Abs(d.x * dir.y - d.y * dir.x);
                float score = along + across * EdgeAlongBias;
                if (score < bestScore) { bestScore = score; best = b; }
            }
            if (best == null) return;
            Board.SetFocus(-1);
            EventSystem.current.SetSelectedGameObject(best.gameObject);
        }

        IEnumerable<Button> HudButtons() { yield return undoBtn; yield return shuffleBtn; yield return potBtn; yield return restartBtn; yield return backBtn; yield return settingsBtn; }

        /// <summary>Puts the focus where it belongs for the current screen: the top popup's first button, the Play button, or the board cursor.</summary>
        void ApplyFocus()
        {
            focusRequested = false;
            var es = EventSystem.current;
            if (es == null || !InputMode.FocusVisible) return;
            if (PopupOpen)
            {
                if (Board != null) Board.SetFocus(-1);
                var top = popups[popups.Count - 1];
                if (top == null) return;
                foreach (var b in top.GetComponentsInChildren<Button>(false)) if (b.interactable) { es.SetSelectedGameObject(b.gameObject); return; }
                return;
            }
            if (!inGame) { Board?.SetFocus(-1); es.SetSelectedGameObject(playBtn.gameObject); return; }
            es.SetSelectedGameObject(null);
            if (Board.Pots.Count > 0) { cursor = Mathf.Clamp(cursor, 0, Board.Pots.Count - 1); Board.SetFocus(cursor); }
        }
    }
}
