namespace Thimadera.StardewMods.StackEverythingRedux.Patches.Size
{
    internal class MaximumStackSizePatch
    {
        public static bool Prefix(ref int __result)
        {
            __result = 999;

            return false;
        }
    }
}
