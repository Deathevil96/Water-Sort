using System;
using PaintSort.Core;

namespace PaintSort.Game
{
    /// <summary>
    /// Renders a short, relaxed marimba / ukulele-style background loop (C - Am - F - G, 92 BPM, 8 bars) as raw samples.
    /// Pure managed math with no Unity calls, so it can run on a worker thread. Note tails wrap around the end of the buffer, so the loop is seamless.
    /// Drop a clip named "music" into Resources/Audio to replace it.
    /// </summary>
    public static class MusicSynth
    {
        public const int SampleRate = 22050;
        const int Bpm = 92, Bars = 8;
        static readonly int[][] Chords = { new[] { 48, 52, 55 }, new[] { 45, 48, 52 }, new[] { 53, 57, 60 }, new[] { 55, 59, 62 } };   // C, Am, F, G (MIDI notes)
        static readonly int[] ArpPattern = { 0, 2, 1, 2, 0, 2, 1, 2 };
        static readonly int[] Pentatonic = { 60, 62, 64, 67, 69, 72, 74 };

        public static float[] Render()
        {
            double beat = 60.0 / Bpm;
            var buffer = new float[(int)(Bars * 4 * beat * SampleRate)];
            var rng = new DetRandom(2024);
            int melodyIndex = 2;
            for (int bar = 0; bar < Bars; bar++)
            {
                var chord = Chords[bar / 2];
                double barStart = bar * 4 * beat;
                for (int e = 0; e < 8; e++) Pluck(buffer, barStart + e * beat * 0.5, chord[ArpPattern[e]] + 12, 0.16f);   // arpeggio
                Pluck(buffer, barStart, chord[0] - 12, 0.30f);                                                              // bass on beats 1 and 3
                Pluck(buffer, barStart + 2 * beat, chord[2] - 12, 0.22f);
                for (int b = 0; b < 4; b++)                                                                                 // sparse random-walk melody
                {
                    if (rng.Range(0, 100) < 40) continue;
                    melodyIndex = Math.Max(0, Math.Min(Pentatonic.Length - 1, melodyIndex + rng.Range(-1, 2)));
                    Pluck(buffer, barStart + b * beat, Pentatonic[melodyIndex] + 12, 0.11f);
                }
            }
            float peak = 0.0001f;
            for (int i = 0; i < buffer.Length; i++) peak = Math.Max(peak, Math.Abs(buffer[i]));
            float gain = 0.6f / peak;
            for (int i = 0; i < buffer.Length; i++) buffer[i] *= gain;
            return buffer;
        }

        static void Pluck(float[] buffer, double startSeconds, int midi, float volume)
        {
            double freq = 440.0 * Math.Pow(2.0, (midi - 69) / 12.0);
            int start = (int)(startSeconds * SampleRate), length = (int)(SampleRate * 1.3);
            for (int i = 0; i < length; i++)
            {
                double t = i / (double)SampleRate;
                double envelope = Math.Exp(-t * 4.5) * (1.0 - Math.Exp(-t * 700.0));                                  // quick attack, woody decay
                double tone = Math.Sin(2 * Math.PI * freq * t)
                            + 0.40 * Math.Sin(4 * Math.PI * freq * t) * Math.Exp(-t * 9.0)
                            + 0.12 * Math.Sin(6 * Math.PI * freq * t) * Math.Exp(-t * 18.0);
                buffer[(start + i) % buffer.Length] += (float)(tone * envelope * volume);
            }
        }
    }
}
