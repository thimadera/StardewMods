using HarmonyLib;
using StardewValley;
using SObject = StardewValley.Object;

namespace StackEverythingRedux.Patches
{
    [HarmonyPatch(typeof(Item), nameof(Item.addToStack))]
    internal static class TackleStackPatch
    {
        private static int preStack;
        private static float prePool;
        private static SObject preObj, otherObj;

        private static void Prefix(Item __instance, Item otherStack)
        {
            preObj = __instance as SObject;
            otherObj = otherStack as SObject;

            if (!IsTackle(preObj, otherObj))
            {
                return;
            }

            preStack = preObj.Stack;
            prePool = DurPool.Read(preObj);
        }

        private static void Postfix(Item __instance, Item otherStack)
        {
            SObject dest = __instance as SObject;
            SObject src = otherStack as SObject;
            if (!IsTackle(dest, src))
            {
                return;
            }

            int moved = dest.Stack - preStack;
            if (moved <= 0)
            {
                return;
            }

            float srcPool = DurPool.Read(src);
            float movedFrac = (src.Stack > 0) ? srcPool * (moved / (float)src.Stack) : 0f;

            DurPool.Write(dest, prePool + movedFrac);
            DurPool.Normalize(dest);

            if (src.Stack > 0)
            {
                DurPool.Write(src, srcPool - movedFrac);
                DurPool.Normalize(src);
            }
        }

        private static bool IsTackle(SObject a, SObject b)
        {
            return a != null && b != null && a.Category == -22 && b.Category == -22 &&
            a.QualifiedItemId == b.QualifiedItemId;
        }
    }
}
