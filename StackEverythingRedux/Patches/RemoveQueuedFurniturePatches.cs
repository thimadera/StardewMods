using HarmonyLib;
using StardewValley;
using StardewValley.Inventories;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace StackEverythingRedux.Patches
{
    internal class RemoveQueuedFurniturePatches
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source, ILGenerator gen)
        {
            CodeMatcher il = new CodeMatcher(source, gen);
            LocalBuilder qualifiedId = gen.DeclareLocal(typeof(string));
            Label skip = gen.DefineLabel();
            Label @break = gen.DefineLabel();

            // FIND: after furniture.TryGetValue();
            il.MatchStartForward(
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldloc_1)
            );

            // ADD: qualifiedId = furniture.QualifiedItemId;
            il.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Callvirt, typeof(Item).GetProperty(nameof(Item.QualifiedItemId)).GetMethod),
                new CodeInstruction(OpCodes.Stloc, qualifiedId)
            );

            // FIND: inv[i] != null
            il.MatchStartForward(
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Callvirt, typeof(Farmer).GetProperty(nameof(Farmer.Items)).GetMethod),
                new CodeMatch(OpCodes.Ldloc_3),
                new CodeMatch(OpCodes.Callvirt, typeof(Inventory).GetMethod("get_Item"))
            );

            // ADD: code chunk
            // setup jumps
            il.Advance(1);
            il.Insert(
                new CodeInstruction(OpCodes.Ldloc_0).WithLabels(skip)
            );

            // if (CheckModifyFurnitureStack(who, i, qualifiedId, ref foundInToolbar))
            il.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Ldloc, qualifiedId),
                new CodeInstruction(OpCodes.Ldloca, 2),
                new CodeInstruction(OpCodes.Call, typeof(RemoveQueuedFurniturePatches).GetMethod(nameof(CheckModifyFurnitureStack)))
            );

            // break;
            il.InsertAndAdvance(
                new CodeInstruction(OpCodes.Brfalse, skip),
                new CodeInstruction(OpCodes.Br, @break)
            );

            // FIND: loop end
            il.MatchStartForward(
                new CodeMatch(OpCodes.Blt_S)
            );
            il.Advance(1);
            il.AddLabels([@break]); // attach break

            return il.InstructionEnumeration();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckModifyFurnitureStack(Farmer who, int i, string qualifiedId, ref bool foundInToolbar)
        {
            if (who.Items[i] is Item item && item.QualifiedItemId == qualifiedId)
            {
                item.Stack++;
                who.CurrentToolIndex = i;
                foundInToolbar = true;
                return true;
            }
            return false;
        }
    }
}
