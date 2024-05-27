using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System.Reflection.Emit;
using SObject = StardewValley.Object;

namespace StackEverythingRedux.Patches
{
    internal class TryToPlaceItemPatches
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source, ILGenerator gen)
        {
            CodeMatcher il = new CodeMatcher(source, gen);

            // FIND: return true;
            il.MatchStartForward(
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Ret)
            );

            // get jump
            List<Label> labels = il.Instruction.labels;
            il.Instruction.labels = null;

            // ADD: AdjustFurnitureStack(item);
            il.Insert(
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, typeof(TryToPlaceItemPatches).GetMethod(nameof(AdjustFurnitureStack)))
            );

            // move jump
            if (labels != null && labels.Count != 0)
                il.Instruction.labels = labels;

            return il.InstructionEnumeration();
        }

        public static void AdjustFurnitureStack(Item item)
        {
            if (item is Furniture f && f.Stack > 1)
            {
                Furniture copy = Copy(f);
                if (copy != null)
                {
                    copy.TileLocation = f.TileLocation;
                    copy.boundingBox.Value = f.boundingBox.Value;
                    copy.defaultBoundingBox.Value = f.defaultBoundingBox.Value;
                    copy.Stack = f.Stack - 1;
                    copy.updateDrawPosition();
                    Game1.player.ActiveObject = copy;
                }

                f.Stack = 1;
            }
        }

        private static Furniture Copy(Furniture obj)
        {
            Furniture furniture = obj.getOne() as Furniture;

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
