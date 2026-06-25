using System;

namespace CategorySorter
{
    /// <summary>
    /// Hilfsfunktionen fuer den Vergleich von Kisten-Namen / Kategorie-Tags.
    /// Eckige Klammern sind optional, Vergleich ist case-insensitive.
    /// "[sort]" == "sort", "[Ammo/Weapons]" == "ammo/weapons".
    /// </summary>
    public static class TagUtil
    {
        /// <summary>Entfernt umschliessende eckige Klammern und Whitespace.</summary>
        public static string Strip(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim();
            if (s.Length >= 2 && s[0] == '[' && s[s.Length - 1] == ']')
                s = s.Substring(1, s.Length - 2).Trim();
            return s;
        }

        /// <summary>True, wenn beide Namen (ohne Klammern, case-insensitive) gleich sind.</summary>
        public static bool TagEquals(string a, string b)
        {
            return string.Equals(Strip(a), Strip(b), StringComparison.OrdinalIgnoreCase);
        }
    }
}
