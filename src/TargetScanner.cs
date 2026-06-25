using System;
using System.Collections.Generic;
using System.Linq;

namespace CategorySorter
{
    /// <summary>
    /// Findet moegliche Ziel-Kisten rund um die Sortierkiste. Uebernommen aus dem bewaehrten
    /// Muster von Relvl/7DTD-SortingChest: 3x3-Chunk-Scan, Radius-Filter, Ausschluss von
    /// gerade geoeffneten / unberuehrten / sich selbst.
    /// </summary>
    public static class TargetScanner
    {
        /// <summary>Ziel-TileEntities, nach Distanz zur Sortierkiste aufsteigend sortiert.</summary>
        public static List<TileEntity> GetPossibleTargets(Vector3i blockPos)
        {
            var possibleTargets = new Dictionary<Vector3i, TileEntity>();
            var chunkX = World.toChunkXZ(blockPos.x);
            var chunkZ = World.toChunkXZ(blockPos.z);

            var distance = Config.MaxDistance < 0 ? 0 : Math.Pow(Config.MaxDistance, 2);

            for (var offX = -1; offX < 2; offX++)
            {
                for (var offZ = -1; offZ < 2; offZ++)
                {
                    if (!(GameManager.Instance.World.GetChunkSync(chunkX + offX, chunkZ + offZ) is Chunk chunk))
                        continue;

                    foreach (var entry in chunk.GetTileEntities().dict)
                    {
                        var targetPos = chunk.ToWorldPos(entry.Key);
                        if (CheckTileEntityTarget(entry, targetPos, blockPos, distance))
                            possibleTargets[targetPos] = entry.Value;
                    }
                }
            }

            return possibleTargets
                .OrderBy(kv => (blockPos.ToVector3() - kv.Key).sqrMagnitude)
                .Select(kv => kv.Value)
                .ToList();
        }

        private static bool CheckTileEntityTarget(
            KeyValuePair<Vector3i, TileEntity> entry, Vector3i targetPos, Vector3i blockPos, double distance)
        {
            // sich selbst ueberspringen
            if (targetPos.Equals(blockPos)) return false;

            // nur Kisten-aehnliche Typen
            if (!Config.AvailableTargetTypes.Contains(entry.Value.GetTileEntityType())) return false;

            // andere Sortierkisten ueberspringen
            if (entry.Value is TileEntityComposite targetComposite)
            {
                var targetSignable = targetComposite.GetFeature<TEFeatureSignable>();
                var tname = targetSignable != null && targetSignable.signText != null ? targetSignable.signText.Text : null;
                if (TagUtil.TagEquals(tname, Config.SortTag)) return false;
            }

            // nicht in unberuehrte Loot-Container schieben (z. B. noch nicht gepluenderte Welt-Container)
            if (entry.Value is TileEntityLootContainer lc && !lc.bTouched) return false;
            if (entry.Value is TileEntitySecureLootContainer slc && !slc.bTouched) return false;

            // zu weit entfernt
            if (distance > 0)
            {
                var distanceSq = (blockPos.ToVector3() - targetPos).sqrMagnitude;
                if (distanceSq > distance) return false;
            }

            // gerade von einem Spieler geoeffnet
            var anotherLockerId = GameManager.Instance.GetEntityIDForLockedTileEntity(entry.Value);
            if (anotherLockerId != -1) return false;

            return true;
        }
    }
}
