using StardewValley;
using StardewValley.Tools;

namespace Thimadera.StardewMods.StackEverythingRedux.Patches
{
    internal class DoDoneFishingPatch
    {
        private static List<StardewValley.Object> tackles;

        public static void Prefix(FishingRod __instance)
        {
            tackles = __instance.GetTackle();
        }

        public static void Postfix(FishingRod __instance)
        {
            if (__instance.attachments is null || __instance.attachments?.Count <= 1)
            {
                return;
            }

            int i = 1;
            foreach (StardewValley.Object tackle in tackles)
            {
                if (tackle != null && __instance.attachments[i] == null)
                {
                    if (tackle.Stack > 1)
                    {
                        tackle.Stack--;
                        tackle.uses.Value = 0;
                        __instance.attachments[i] = tackle;

                        string displayedMessage = new HUDMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingRod.cs.14086")).message;
                        _ = Game1.hudMessages.Remove(Game1.hudMessages.FirstOrDefault(item => item.message == displayedMessage));
                    }
                }
                i++;
            }
        }
    }
}
