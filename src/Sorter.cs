using System;
using System.Collections.Generic;
using System.Linq;

namespace CategorySorter
{
    /// <summary>
    /// Kernlogik: Verteilt die Items der Sortierkiste auf Kategorie-Kisten in der Naehe.
    /// Eine Ziel-Kiste gilt fuer ein Item als passend, wenn ihr (klammernfreier) Name einer
    /// der Item-Kategorien (ItemClass.Groups) entspricht. Items ohne passende Kategorie-Kiste
    /// wandern in eine optionale [misc]-Sammelkiste, sonst bleiben sie liegen.
    /// </summary>
    public static class Sorter
    {
        private static readonly string[] NoGroups = new string[0];

        /// <summary>Ein moegliches Ziel mit seiner Kategorie (null = Sammelkiste).</summary>
        private sealed class TargetBox
        {
            public readonly ITileEntityLootable Lootable;
            public readonly string Category; // bereits klammernfrei

            public TargetBox(ITileEntityLootable lootable, string category)
            {
                Lootable = lootable;
                Category = category;
            }
        }

        public static void DoSortingOut(TileEntity chest, Vector3i blockPos, EntityPlayer player)
        {
            if (chest == null || player == null) return;
            if (!(chest is TileEntityComposite composite)) return;

            var signable = composite.GetFeature<TEFeatureSignable>();
            var storage = composite.GetFeature<TEFeatureStorage>();
            if (storage == null) return;

            // Nur die als Sortierkiste benannte Kiste verarbeiten.
            var sourceName = signable != null && signable.signText != null ? signable.signText.Text : null;
            if (!TagUtil.TagEquals(sourceName, Config.SortTag)) return;

            var stacksBefore = storage.items.Count(i => !i.IsEmpty());
            if (stacksBefore == 0) return;

            // Ziel-Kisten finden und nach Kategorie / Sammelkiste aufteilen (Distanzreihenfolge bleibt erhalten).
            var categoryTargets = new List<TargetBox>();
            var miscTargets = new List<TargetBox>();

            foreach (var te in TargetScanner.GetPossibleTargets(blockPos))
            {
                if (!te.TryGetSelfOrFeature<ITileEntityLootable>(out var lootable)) continue;
                if (lootable.IsUserAccessing()) continue;

                var tname = (te as TileEntityComposite)?.GetFeature<TEFeatureSignable>()?.signText?.Text;
                if (string.IsNullOrEmpty(tname)) continue;
                if (TagUtil.TagEquals(tname, Config.SortTag)) continue;

                if (TagUtil.TagEquals(tname, Config.MiscTag))
                    miscTargets.Add(new TargetBox(lootable, null));
                else
                    categoryTargets.Add(new TargetBox(lootable, TagUtil.Strip(tname)));
            }

            var accessed = new HashSet<ITileEntityLootable>(); // wir haben SetUserAccessing(true) gesetzt
            var modified = new HashSet<ITileEntityLootable>(); // wirklich veraendert

            try
            {
                for (var i = 0; i < storage.items.Length; i++)
                {
                    var stack = storage.items[i];
                    if (stack.IsEmpty()) continue;

                    var groups = GetGroups(stack);

                    // 1) naechste passende Kategorie-Kiste, die das Item aufnimmt
                    var moved = false;
                    foreach (var t in categoryTargets)
                    {
                        if (!groups.Any(g => string.Equals(g.Trim(), t.Category, StringComparison.OrdinalIgnoreCase)))
                            continue;
                        if (MoveInto(t.Lootable, stack, accessed, modified))
                        {
                            moved = true;
                            break;
                        }
                    }

                    // 2) Fallback: Sammelkiste
                    if (!moved)
                    {
                        foreach (var t in miscTargets)
                        {
                            if (MoveInto(t.Lootable, stack, accessed, modified))
                            {
                                moved = true;
                                break;
                            }
                        }
                    }

                    if (moved)
                        storage.UpdateSlot(i, ItemStack.Empty);
                }
            }
            finally
            {
                foreach (var lootable in accessed)
                {
                    if (modified.Contains(lootable)) lootable.SetModified();
                    lootable.SetUserAccessing(false);
                }
            }

            if (modified.Count > 0) storage.SetModified();

            SendFeedback(player, stacksBefore, storage.items.Count(i => !i.IsEmpty()));
        }

        private static string[] GetGroups(ItemStack stack)
        {
            var itemClass = ItemClass.GetForId(stack.itemValue.type);
            if (itemClass == null || itemClass.Groups == null) return NoGroups;
            return itemClass.Groups;
        }

        private static bool MoveInto(
            ITileEntityLootable lootable, ItemStack stack,
            HashSet<ITileEntityLootable> accessed, HashSet<ITileEntityLootable> modified)
        {
            if (accessed.Add(lootable))
                lootable.SetUserAccessing(true); // beim ersten Zugriff sperren

            // Stapelbare Items zuerst auf vorhandene Teilstapel legen; nicht stapelbare Items
            // (Werkzeuge, Waffen, Ruestung mit Haltbarkeit/Mods) liefern hier allMoved=false und
            // werden ueber AddItem in einen freien Slot gelegt - dabei bleibt der ItemValue
            // (Haltbarkeit, angebrachte Mods) erhalten.
            bool moved;
            if (lootable.TryStackItem(0, stack).allMoved)
                moved = true;
            else
                moved = lootable.AddItem(stack);

            if (moved) modified.Add(lootable);
            return moved;
        }

        private static void SendFeedback(EntityPlayer player, int stacksBefore, int rest)
        {
            string msg;
            if (rest == stacksBefore)
                msg = "[CategorySorter] Nothing sorted - no matching category chest found.";
            else if (rest > 0)
                msg = string.Format("[CategorySorter] Sorted {0} stack(s), {1} left without a target.", stacksBefore - rest, rest);
            else
                msg = string.Format("[CategorySorter] Sorted all {0} stack(s).", stacksBefore);

            GameManager.Instance.ChatMessageServer(
                null, EChatType.Whisper, -1, msg, new List<int> { player.entityId }, EMessageSender.Server);
        }
    }
}
