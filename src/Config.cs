using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

namespace CategorySorter
{
    /// <summary>
    /// Laedt die Konfiguration aus Mods/CategorySorter/Config/CategorySorter.xml.
    /// Faellt bei Fehlern auf die Defaults zurueck.
    /// </summary>
    public static class Config
    {
        public static string AssemblyName
        {
            get { return Assembly.GetExecutingAssembly().GetName().Name; }
        }

        /// <summary>Name der Sortierkiste (Klammern optional, case-insensitive).</summary>
        public static string SortTag { get; private set; } = "[sort]";

        /// <summary>Name der optionalen Sammelkiste fuer nicht zuordenbare Items.</summary>
        public static string MiscTag { get; private set; } = "[misc]";

        /// <summary>
        /// Optionale Alias-Tabelle: Kisten-Label (klammernfrei) -> eine oder mehrere echte
        /// Kategorien. Erlaubt frei waehlbare Wunschnamen zusaetzlich zum automatischen
        /// Slash-Segment-Matching. Case-insensitive.
        /// </summary>
        public static IDictionary<string, string[]> Aliases { get; private set; }
            = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Radius in Bloecken, in dem nach Ziel-Kisten gesucht wird. -1 = volle 3x3-Chunks.</summary>
        public static float MaxDistance { get; private set; } = 20f;

        public static bool VerboseLogging { get; private set; } = false;

        /// <summary>TileEntity-Typen, die als Ziel-Kisten gelten.</summary>
        public static readonly List<TileEntityType> AvailableTargetTypes = new List<TileEntityType>
        {
            TileEntityType.Loot,
            TileEntityType.SecureLoot,
            TileEntityType.SecureLootSigned,
            TileEntityType.Composite
        };

        public static void Load()
        {
            try
            {
                var path = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Mods/" + AssemblyName + "/Config/" + AssemblyName + ".xml");

                if (!File.Exists(path))
                {
                    Log.Warning("[" + AssemblyName + "] Config nicht gefunden (" + path + "), nutze Defaults.");
                    return;
                }

                var doc = new XmlDocument();
                doc.Load(path);
                var root = doc.DocumentElement;
                if (root == null)
                {
                    Log.Warning("[" + AssemblyName + "] Leere Config, nutze Defaults.");
                    return;
                }

                var verbose = root["VerboseLogging"];
                if (verbose != null && !string.IsNullOrEmpty(verbose.InnerText))
                    VerboseLogging = bool.Parse(verbose.InnerText.Trim());

                var dist = root["MaxDistance"];
                if (dist != null && !string.IsNullOrEmpty(dist.InnerText))
                    MaxDistance = float.Parse(dist.InnerText.Trim(), CultureInfo.InvariantCulture);

                var sortTag = root["SortTag"];
                if (sortTag != null && !string.IsNullOrEmpty(sortTag.InnerText))
                    SortTag = sortTag.InnerText.Trim();

                var miscTag = root["MiscTag"];
                if (miscTag != null && !string.IsNullOrEmpty(miscTag.InnerText))
                    MiscTag = miscTag.InnerText.Trim();

                LoadAliases(root["Aliases"]);

                Log.Out("[" + AssemblyName + "] Config geladen: SortTag=" + SortTag
                    + ", MiscTag=" + MiscTag + ", MaxDistance=" + MaxDistance
                    + ", Aliases=" + Aliases.Count
                    + ", VerboseLogging=" + VerboseLogging);
            }
            catch (Exception e)
            {
                Log.Error("[" + AssemblyName + "] Config-Fehler: " + e.Message);
            }
        }

        /// <summary>
        /// Liest &lt;Aliases&gt;&lt;Alias label="Guns" group="Ranged Weapons"/&gt;...&lt;/Aliases&gt;.
        /// "group" darf mehrere echte Kategorien per Komma enthalten. Label-Klammern werden entfernt.
        /// </summary>
        private static void LoadAliases(XmlElement aliasesRoot)
        {
            if (aliasesRoot == null) return;

            var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlNode node in aliasesRoot.ChildNodes)
            {
                if (!(node is XmlElement el)) continue;
                if (!string.Equals(el.Name, "Alias", StringComparison.OrdinalIgnoreCase)) continue;

                var label = TagUtil.Strip(el.GetAttribute("label"));
                var groupAttr = el.GetAttribute("group");
                if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(groupAttr)) continue;

                var groups = new List<string>();
                foreach (var g in groupAttr.Split(','))
                {
                    var trimmed = g.Trim();
                    if (trimmed.Length > 0) groups.Add(trimmed);
                }
                if (groups.Count > 0) map[label] = groups.ToArray();
            }

            Aliases = map;
        }
    }
}
