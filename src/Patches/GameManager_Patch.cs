using System;
using HarmonyLib;

namespace CategorySorter
{
    /// <summary>
    /// Haengt sich an GameManager.TEUnlockServer ein. Diese Methode wird serverseitig
    /// aufgerufen, sobald ein Spieler eine Kiste schliesst (entsperrt). Wir ermitteln die
    /// geschlossene TileEntity und uebergeben sie an den Sorter, der prueft, ob es die
    /// Sortierkiste ist.
    /// </summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.TEUnlockServer))]
    public static class GameManager_TEUnlockServer_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(int _clrIdx, Vector3i _blockPos, int _lootEntityId, bool _allowContainerDestroy)
        {
            if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer) return;

            try
            {
                TileEntity chest = null;
                var lockerId = -1;

                // Die schliessende TileEntity ueber die Liste der gerade gesperrten Kisten finden.
                if (_lootEntityId == -1)
                {
                    chest = GameManager.Instance.World.GetTileEntity(_blockPos);
                    foreach (var entry in GameManager.Instance.lockedTileEntities)
                    {
                        if (entry.Key is TileEntity te && entry.Key.ToWorldPos().Equals(_blockPos))
                        {
                            chest = te;
                            lockerId = entry.Value;
                        }
                    }
                }
                else
                {
                    foreach (var entry in GameManager.Instance.lockedTileEntities)
                    {
                        if (entry.Key.EntityId == _lootEntityId && entry.Key is TileEntity te)
                        {
                            chest = te;
                            lockerId = entry.Value;
                        }
                    }
                }

                if (lockerId == -1) return;
                if (!GameManager.Instance.World.Players.dict.TryGetValue(lockerId, out var player)) return;

                Sorter.DoSortingOut(chest, _blockPos, player);
            }
            catch (Exception e)
            {
                Log.Error("[" + ModApi.ModName + "] " + e);
            }
        }
    }
}
