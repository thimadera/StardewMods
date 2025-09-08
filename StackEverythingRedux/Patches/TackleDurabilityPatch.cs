using HarmonyLib;
using StardewValley;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace StackEverythingRedux.Patches
{
    [HarmonyPatch(typeof(FishingRod), "doDoneFishing")]
    internal static class TackleDurabilityPatch
    {
        private static bool origLastCatchWasJunk;

        private static void Prefix(FishingRod __instance, ref bool consumeBaitAndTackle)
        {
            if (!consumeBaitAndTackle)
            {
                return;
            }

            // salvar estado original
            origLastCatchWasJunk = __instance.lastCatchWasJunk;
            // força junk = true pra bloquear consumo vanilla dos anzóis
            __instance.lastCatchWasJunk = true;

            Farmer who = __instance.getLastFarmerToUse();
            float consumeChance = who?.stats.Get("Preserving") > 0 ? 0.5f : 1f;

            for (int i = 1; i < __instance.attachments.Count; i++)
            {
                if (__instance.attachments[i] is not SObject tackle || tackle.Category != -22)
                {
                    continue;
                }

                if (origLastCatchWasJunk || Game1.random.NextDouble() >= consumeChance)
                {
                    continue;
                }

                float pool = DurPool.Read(tackle);
                pool -= 1f / DurPool.MaxUses(tackle);

                if (pool <= 0f)
                {
                    __instance.attachments[i] = null;
                    if (who?.IsLocalPlayer == true)
                    {
                        Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingRod.cs.14067"));
                    }
                }
                else
                {
                    DurPool.Write(tackle, pool);
                    DurPool.Normalize(tackle);
                }
            }
        }

        private static void Postfix(FishingRod __instance, ref bool consumeBaitAndTackle)
        {
            if (consumeBaitAndTackle)
            {
                __instance.lastCatchWasJunk = origLastCatchWasJunk;
            }
        }
    }
}
