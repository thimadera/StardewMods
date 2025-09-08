using HarmonyLib;
using StardewValley;
using StardewValley.Objects;

namespace StackEverythingRedux.Patches
{
    [HarmonyPatch(typeof(Furniture))]
    internal static class FurniturePlacePatch
    {
        private struct State
        {
            public int Slot;
            public int PreStack;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Furniture.placementAction))]
        private static void Prefix(Furniture __instance, Farmer who, ref State __state)
        {
            __state = default;

            if (who == null || __instance == null)
            {
                return;
            }

            __state.PreStack = __instance.Stack;

            Log.Debug($"[Place] Pre: name='{__instance.DisplayName}', preStack={__state.PreStack}, currentToolIndex={who.CurrentToolIndex}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Furniture.placementAction))]
        private static void Postfix(Furniture __instance, GameLocation location, int x, int y, Farmer who, ref bool __result, State __state)
        {
            if (!__result || who == null)
            {
                return;
            }

            int pre = __state.PreStack;

            if (pre <= 1)
            {
                return;
            }

            Item back = __instance.getOne();
            back.Stack = pre;

            __instance.Stack = 1;

            int slot = who.CurrentToolIndex;
            who.Items[slot] = back;
            Log.Debug($"[Place] Wrote remainder {back.Stack} back to active slot {slot} for '{__instance.DisplayName}'.");
        }
    }
}
