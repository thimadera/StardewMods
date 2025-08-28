using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using StardewValley;
using StardewValley.Objects;

namespace StackEverythingRedux.Patches
{
    internal class TryToPlaceItemPatches
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source, ILGenerator gen)
        {
            CodeMatcher il = new(source, gen);

            _ = il.MatchStartForward(
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Ret)
            );

            List<Label> labels = il.Instruction.labels;
            il.Instruction.labels = null;

            _ = il.Insert(
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TryToPlaceItemPatches), nameof(AdjustFurnitureStack)))
            );

            if (labels is { Count: > 0 })
            {
                il.Instruction.labels = labels;
            }

            Log.Debug("[TryToPlaceItem/Transpiler] injected AdjustFurnitureStack before 'return true'.");
            return il.InstructionEnumeration();
        }

        private static string Desc(Item it)
        {
            return it is null
                ? "null"
                : it is Object o ? $"{o.Name} qid={o.QualifiedItemId} stack={o.Stack}" : $"{it.DisplayName} stack={it.Stack}";
        }

        public static void AdjustFurnitureStack(Item item)
        {
            Log.Debug($"[TryToPlaceItem] AdjustFurnitureStack in | item={Desc(item)}");

            if (item is Furniture f && f.Stack > 1)
            {
                Log.Debug($"[TryToPlaceItem] furniture stack>1, splitting: {f.Name} x{f.Stack}");

                Furniture copy = Copy(f);
                if (copy != null)
                {
                    copy.TileLocation = f.TileLocation;
                    copy.boundingBox.Value = f.boundingBox.Value;
                    copy.defaultBoundingBox.Value = f.defaultBoundingBox.Value;
                    copy.Stack = f.Stack - 1;
                    copy.updateDrawPosition();

                    Game1.player.ActiveObject = copy;
                    Log.Debug($"[TryToPlaceItem] returned to hand: {copy.Name} x{copy.Stack}");
                }
                else
                {
                    Log.Warn("[TryToPlaceItem] Copy(furniture) returned null; skipping.");
                }

                f.Stack = 1;
                Log.Debug($"[TryToPlaceItem] placed furniture now has stack={f.Stack}");
            }

            Log.Trace($"[TryToPlaceItem] AdjustFurnitureStack out | item={Desc(item)}");
        }

        private static Furniture Copy(Furniture obj)
        {
            if (obj.getOne() is not Furniture furniture)
            {
                Log.Warn("[TryToPlaceItem] getOne() returned null for furniture.");
                return null;
            }

            int attempts = 0;
            while (!furniture.boundingBox.Value.Equals(obj.boundingBox.Value) && attempts < 8)
            {
                furniture.rotate();
                attempts++;
            }

            return furniture;
        }
    }
}
