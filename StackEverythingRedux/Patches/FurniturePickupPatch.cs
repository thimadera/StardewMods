using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

namespace Thimadera.StardewMods.StackEverythingRedux.Patches
{
    internal class FurniturePickupPatch
    {
        public static bool Prefix(DecoratableLocation __instance, Guid guid)
        {
            RemoveQueuedFurniture(__instance, guid);
            return false;
        }

        private static void RemoveQueuedFurniture(DecoratableLocation instance, Guid guid)
        {
            Farmer player = Game1.player;
            if (!instance.furniture.ContainsGuid(guid))
            {
                return;
            }

            Furniture furniture = instance.furniture[guid];
            if (!player.couldInventoryAcceptThisItem(furniture))
            {
                return;
            }

            furniture.performRemoveAction();
            instance.furniture.Remove(guid);

            Item result = player.addItemToInventory(furniture);

            if (result != null)
            {
                _ = Game1.createItemDebris(result, player.Position, player.FacingDirection);
            }
            else
            {
                player.CurrentToolIndex = player.getIndexOfInventoryItem(furniture);
            }
            instance.localSound("coin");
        }
    }
}
