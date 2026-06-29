using System;
using System.Collections.Generic;

namespace CategorySorter
{
    /// <summary>
    /// Entscheidet, ob ein Kisten-Label zu den Kategorien (ItemClass.Groups) eines Items passt.
    ///
    /// In 7DTD V2.6 stammen die Kategorien aus der Item-Property "Group" der items.xml
    /// (z. B. "Ammo/Weapons,Ranged Weapons"); ItemClass splittet sie an Kommas in ItemClass.Groups.
    /// Matching-Regeln:
    ///  - Alias: Steht das Label in der Alias-Tabelle der Config, gelten dessen Ziel-Kategorien.
    ///  - Slash-Segmente: "Food/Cooking" passt auf [Food] ODER [Cooking] (und auf [Food/Cooking]).
    ///    Damit funktionieren kurze Labels ohne Konfiguration.
    ///  - Interne Filter-Tokens des Spiels (CF*, TC*, *Only, advBuilding) werden ignoriert.
    /// Vergleich immer case-insensitive; eckige Klammern sind zu diesem Zeitpunkt bereits entfernt.
    /// </summary>
    public static class CategoryMatcher
    {
        /// <summary>True, wenn das (klammernfreie) Kisten-Label eine der Item-Kategorien trifft.</summary>
        public static bool Matches(string chestLabel, string[] itemGroups, IDictionary<string, string[]> aliases)
        {
            if (string.IsNullOrEmpty(chestLabel) || itemGroups == null) return false;

            // Label ggf. ueber Alias auf echte Kategorie-Begriffe aufloesen, sonst woertlich nehmen.
            string[] terms;
            if (aliases != null && aliases.TryGetValue(chestLabel.Trim(), out var aliased) && aliased.Length > 0)
                terms = aliased;
            else
                terms = new[] { chestLabel };

            foreach (var group in itemGroups)
            {
                if (string.IsNullOrEmpty(group)) continue;
                var token = group.Trim();
                if (IsInternalToken(token)) continue;

                // Ganzes Token und seine Slash-Segmente gelten als matchbare Begriffe.
                if (TermMatches(terms, token)) return true;
                if (token.IndexOf('/') >= 0)
                {
                    foreach (var seg in token.Split('/'))
                        if (TermMatches(terms, seg.Trim())) return true;
                }
            }
            return false;
        }

        private static bool TermMatches(string[] terms, string candidate)
        {
            if (string.IsNullOrEmpty(candidate)) return false;
            foreach (var t in terms)
                if (string.Equals(t.Trim(), candidate, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        /// <summary>
        /// Interne Crafting-/Trader-Filter-Tokens, die V2.6 in "Group" mitfuehrt, aber nie als
        /// sichtbare Kategorie gedacht sind: CF* (CFFood/Cooking ...), TC* (TCReading ...),
        /// *Only (BooksOnly, SchematicsOnly) und advBuilding. Keine echte Kategorie beginnt mit
        /// "CF"/"TC" oder endet auf "Only", daher ist dieses Aussortieren gefahrlos.
        /// </summary>
        public static bool IsInternalToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return true;
            if (token.StartsWith("CF", StringComparison.Ordinal)) return true;
            if (token.StartsWith("TC", StringComparison.Ordinal)) return true;
            if (token.EndsWith("Only", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(token, "advBuilding", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
