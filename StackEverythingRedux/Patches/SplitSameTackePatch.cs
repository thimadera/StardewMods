using HarmonyLib;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace StackEverythingRedux.Patches
{
    [HarmonyPatch(typeof(FishingRod))]
    internal static class SplitSameTacklePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("canThisBeAttached")]
        private static void Postfix(FishingRod __instance, SObject o, int slot, ref bool __result)
        {
            if (!__result)
            {
                return;
            }

            if (slot == 0 || o is null)
            {
                return;
            }

            if (o.Category != -22)
            {
                return;
            }

            if (!__instance.CanUseTackle())
            {
                return;
            }

            Netcode.NetObjectArray<SObject> attachments = __instance.attachments;
            SObject current = attachments?[slot];
            if (current is not SObject cur)
            {
                return;
            }

            if (cur.QualifiedItemId != o.QualifiedItemId)
            {
                return;
            }

            for (int i = 1; i < __instance.AttachmentSlotsCount; i++)
            {
                if (i == slot)
                {
                    continue;
                }

                if (attachments[i] == null)
                {
                    __result = false;
                    Log.Debug($"[TackleSplit] negated slot {slot}, redirecting to empty slot {i}.");
                    break;
                }
            }
        }
    }
}
