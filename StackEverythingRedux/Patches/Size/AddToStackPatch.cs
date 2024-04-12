using StardewValley;

namespace Thimadera.StardewMods.StackEverythingRedux.Patches.Size
{
    internal class AddToStackPatch
    {
        public static int Prefix(Item __instance, ref Item otherStack, ref int __result)
        {
            if (__instance.Stack is (-1) or 0)
            {
                __instance.Stack = 1;
            }

            if (otherStack.Stack is (-1) or 0)
            {
                otherStack.Stack = 1;
            }

            int maxStack = __instance.maximumStackSize();
            if (maxStack != 1)
            {
                __instance.stack.Value += otherStack.Stack;
                if (__instance is StardewValley.Object obj && otherStack is StardewValley.Object otherObject && obj.IsSpawnedObject && !otherObject.IsSpawnedObject)
                {
                    obj.IsSpawnedObject = false;
                }
                if (__instance.stack.Value > maxStack)
                {
                    _ = __instance.stack.Value - maxStack;
                    __instance.stack.Value = maxStack;
                    return __result;
                }
                return 0;
            }
            return otherStack.Stack;
        }
    }
}
