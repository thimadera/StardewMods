using HarmonyLib;
using StardewValley;
using StardewValley.Objects;

namespace StackEverythingRedux.Patches
{
    [HarmonyPatch(typeof(GameLocation), "removeQueuedFurniture")]
    internal static class RemoveQueuedFurnitureMergePostfix
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            Farmer who = Game1.player;
            if (who == null)
            {
                return;
            }

            int max = StackEverythingRedux.Config?.MaxStackingNumber ?? 1;
            if (max <= 1)
            {
                return;
            }

            StardewValley.Inventories.Inventory items = who.Items;
            int active = who.CurrentToolIndex;
            bool activeAdjusted = false;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is not Furniture a)
                {
                    continue;
                }

                for (int j = i + 1; j < items.Count && a.Stack < max; j++)
                {
                    if (items[j] is not Furniture b)
                    {
                        continue;
                    }

                    if (b.ParentSheetIndex != a.ParentSheetIndex)
                    {
                        continue;
                    }

                    int room = max - a.Stack;
                    if (room <= 0)
                    {
                        break;
                    }

                    int moved = b.Stack <= room ? b.Stack : room;
                    if (moved <= 0)
                    {
                        continue;
                    }

                    a.Stack += moved;
                    b.Stack -= moved;

                    // if we merged away from the active slot, move the selection to the destination
                    if (!activeAdjusted && j == active)
                    {
                        who.CurrentToolIndex = i;
                        activeAdjusted = true;
                        Log.Debug($"[PickupMerge] Moved selection from slot {j} → {i}");
                    }

                    if (b.Stack == 0)
                    {
                        items[j] = null;

                        // if active now points to null (and we haven't fixed it yet), point to i
                        if (!activeAdjusted && j == active)
                        {
                            who.CurrentToolIndex = i;
                            activeAdjusted = true;
                            Log.Debug($"[PickupMerge] Active cleared at {j}, set selection to {i}");
                        }
                    }

                    Log.Debug($"[PickupMerge] PSI={a.ParentSheetIndex} moved {moved} from slot {j} → {i} ({a.Stack}/{max})");
                }
            }

            // final safety: if active points to null, snap to first non-null
            if (who.CurrentToolIndex >= 0 && who.CurrentToolIndex < items.Count && items[who.CurrentToolIndex] == null)
            {
                for (int k = 0; k < items.Count; k++)
                {
                    if (items[k] != null)
                    {
                        who.CurrentToolIndex = k;
                        Log.Debug($"[PickupMerge] Active was null, snapped to slot {k}");
                        break;
                    }
                }
            }
        }
    }
}
