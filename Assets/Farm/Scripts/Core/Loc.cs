using System;
using System.Collections.Generic;
using UnityEngine;

namespace Farm
{
    /// <summary>
    /// Tiny localization layer. The game is written in Vietnamese; every Vietnamese UI string is a key and
    /// <see cref="En"/> (Loc.En.cs) maps it to English. Language is stored in PlayerPrefs ("lang"); changing it
    /// reloads the scene so every window and label is rebuilt in the new language.
    /// </summary>
    public static partial class L
    {
        private const string Pref = "lang";
        private static bool? en;

        /// <summary>True when the game is shown in English.</summary>
        public static bool En
        {
            get
            {
                if (en == null)
                {
                    string saved = PlayerPrefs.GetString(Pref, "");
                    en = saved == "" ? Application.systemLanguage != SystemLanguage.Vietnamese : saved == "en";
                }
                return en.Value;
            }
        }

        public static string Code => En ? "en" : "vi";

        public static void Set(bool english)
        {
            en = english;
            PlayerPrefs.SetString(Pref, english ? "en" : "vi");
            PlayerPrefs.Save();
        }

        /// <summary>Translate a Vietnamese string (exact match). Unknown strings are returned unchanged.</summary>
        public static string T(string vi)
        {
            if (!En || string.IsNullOrEmpty(vi)) return vi;
            return English.TryGetValue(vi, out string t) ? t : vi;
        }

        /// <summary>Translate a Vietnamese format template ({0}, {1:N0} …) then fill it. String arguments
        /// (item names, people…) are translated too.</summary>
        public static string F(string viTemplate, params object[] args)
        {
            string tpl = T(viTemplate);
            if (En && args != null)
                for (int i = 0; i < args.Length; i++)
                    if (args[i] is string s) args[i] = T(s);
            try { return string.Format(tpl, args); }
            catch (FormatException) { return string.Format(viTemplate, args); }
        }
    }
}
