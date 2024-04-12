using StardewValley;
using StardewValley.Tools;

namespace Thimadera.StardewMods.StackEverythingRedux.Patches
{
    internal class DoDoneFishingPatch
    {
        private static StardewValley.Object tackle;

        public static void Prefix(FishingRod __instance)
        {
            tackle = __instance.attachments?.Count > 1 ? __instance.attachments[1] : null;
        }

        public static void Postfix(FishingRod __instance)
        {
            if (__instance.attachments is null || __instance.attachments?.Count <= 1)
            {
                return;
            }

            if (tackle != null && __instance.attachments[1] == null)
            {
                if (tackle.Stack > 1)
                {
                    tackle.Stack--;
                    tackle.uses.Value = 0;
                    __instance.attachments[1] = tackle;

                    string displayedMessage = new HUDMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingRod.cs.14086")).message;
                    _ = Game1.hudMessages.Remove(Game1.hudMessages.FirstOrDefault(item => item.message == displayedMessage));
                }
            }
        }
    }
}