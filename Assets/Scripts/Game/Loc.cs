using System.Collections.Generic;
using UnityEngine;

namespace PaintSort.Game
{
    /// <summary>
    /// Minimal localization. The English text itself is the key, so code stays readable and missing translations fall back to English.
    /// Translations live in Resources/Lang/&lt;code&gt;.txt as tab-separated "English text&lt;TAB&gt;Translation" lines ('#' starts a comment).
    /// Texts built with <c>UI.Txt</c> are translated automatically; texts that contain numbers use <see cref="F"/> with a "{0}" template.
    /// To add a language: add its code and name below and drop a translation file in Resources/Lang.
    /// </summary>
    public static class Loc
    {
        public static readonly string[] Codes = { "en", "es" };
        public static readonly string[] Names = { "English", "Español" };

        static readonly Dictionary<string, string> table = new Dictionary<string, string>();
        public static string Language { get; private set; } = "en";

        public static void SetLanguage(string code)
        {
            table.Clear();
            Language = System.Array.IndexOf(Codes, code) >= 0 ? code : "en";
            if (Language == "en") return;
            var asset = Resources.Load<TextAsset>("Lang/" + Language);
            if (asset == null) { Log.W("LOC", "Missing translation file for " + Language); return; }
            foreach (var raw in asset.text.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (line.Length == 0 || line[0] == '#') continue;
                int tab = line.IndexOf('\t');
                if (tab > 0) table[line.Substring(0, tab)] = line.Substring(tab + 1);
            }
        }

        public static string NextLanguage() => Codes[(System.Array.IndexOf(Codes, Language) + 1) % Codes.Length];
        public static string NameOf(string code) { int i = System.Array.IndexOf(Codes, code); return i >= 0 ? Names[i] : code; }

        public static string T(string english) => english != null && table.TryGetValue(english, out var t) ? t : english;
        public static string F(string englishTemplate, params object[] args) => string.Format(T(englishTemplate), args);
    }
}
