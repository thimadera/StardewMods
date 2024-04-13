namespace Thimadera.StardewMods.StackEverythingRedux.Patches.Size
{
    internal class MaximumStackSizePatch
    {
        public static bool Prefix(ref int __result)
        {
            __result = StackEverythingRedux.Config.MaxStackingNumber;

            return false;
        }
    }
}
