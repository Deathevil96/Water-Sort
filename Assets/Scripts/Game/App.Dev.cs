#if UNITY_EDITOR || DEBUG
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PaintSort.Game
{
    /// <summary>Development-only tools: the debug-menu toggle button and a small performance / layout readout. Compiled out of release builds.</summary>
    public sealed partial class App
    {
        DebugMenu debugMenu; Text devText; float devAccum, devWorst; int devFrames;
        ProfilerRecorder batchesRec, setPassRec, gcRec; long devGcBytes; long devGcMax; long devGcMin = long.MaxValue;

        void OnDestroy() { batchesRec.Dispose(); setPassRec.Dispose(); gcRec.Dispose(); }

        void BuildDevTools()
        {
            debugMenu = gameObject.AddComponent<DebugMenu>(); debugMenu.enabled = false;      // OnGUI only costs anything while the panel is open
            batchesRec = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            setPassRec = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            gcRec = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");

            var tools = UI.Rect(safeRoot, "DevTools"); UI.Stretch(tools); UI.SubCanvas(tools);
            var btn = UI.Btn(tools, "DBG", UI.Grey, new Vector2(110, 64), () => debugMenu.enabled = !debugMenu.enabled, null, 28);
            UI.Place((RectTransform)btn.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(6, 140), new Vector2(110, 64));   // bottom-left, above the banner: clear of the board in every layout
            devText = UI.Txt(tools, "", 26, new Color(0.2f, 0.12f, 0.3f, 0.9f), TextAnchor.LowerRight, false);
            UI.Place(devText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 8), new Vector2(1000, 40));
            devText.raycastTarget = false;
        }

        /// <summary>Called every frame in dev builds: F1 / backquote / three-finger tap toggles the panel; the readout refreshes twice a second.</summary>
        void UpdateDevTools()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.f1Key.wasPressedThisFrame || kb.backquoteKey.wasPressedThisFrame)) debugMenu.enabled = !debugMenu.enabled;
            var touch = Touchscreen.current;
            if (touch != null && touch.touches.Count >= 3 && touch.touches[2].press.wasPressedThisFrame) debugMenu.enabled = !debugMenu.enabled;

            float dt = Time.unscaledDeltaTime;
            devAccum += dt; devFrames++; if (dt > devWorst) devWorst = dt;
            if (gcRec.Valid) { long g = gcRec.LastValue; devGcBytes += g; if (g > devGcMax) devGcMax = g; if (g < devGcMin) devGcMin = g; }
            if (devAccum < 0.5f) return;
            float avg = devAccum / devFrames;
            devText.text = Mathf.RoundToInt(1f / avg) + " fps  worst " + Mathf.RoundToInt(devWorst * 1000f) + " ms  batches " + (batchesRec.Valid ? batchesRec.LastValue : 0)
                         + "  setpass " + (setPassRec.Valid ? setPassRec.LastValue : 0) + "  GC " + (devGcBytes / devFrames) + " B/frame (min " + devGcMin + ", max " + devGcMax + ")  "
                         + Screen.width + "x" + Screen.height + " " + Device + (IsLandscape ? " landscape" : " portrait");
            devAccum = 0; devFrames = 0; devWorst = 0; devGcBytes = 0; devGcMax = 0; devGcMin = long.MaxValue;
        }

        /// <summary>QA: pretend to be an Android TV (landscape layout, no banner, focus ring always on).</summary>
        public void DebugSetTv(bool on) { forceTv = on; DetectDevice(); if (!on) { InputMode.FocusVisible = false; if (Board != null) Board.SetFocus(-1); } ApplyScreen(); RequestFocus(); }
    }
}
#endif
