using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using StardewValley;
using StardewValley.Inventories;

namespace StackEverythingRedux.Patches
{
    internal class RemoveQueuedFurniturePatches
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source, ILGenerator gen)
        {
            CodeMatcher il = new(source, gen);
            LocalBuilder qualifiedId = gen.DeclareLocal(typeof(string));
            Label skip = gen.DefineLabel();
            Label @break = gen.DefineLabel();

            _ = il.MatchStartForward(
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldloc_1)
            );

            _ = il.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Callvirt, typeof(Item).GetProperty(nameof(Item.QualifiedItemId)).GetMethod),
                new CodeInstruction(OpCodes.Stloc, qualifiedId)
            );

            _ = il.MatchStartForward(
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Callvirt, typeof(Farmer).GetProperty(nameof(Farmer.Items)).GetMethod),
                new CodeMatch(OpCodes.Ldloc_3),
                new CodeMatch(OpCodes.Callvirt, typeof(Inventory).GetMethod("get_Item"))
            );

            _ = il.Advance(1);
            _ = il.Insert(
                new CodeInstruction(OpCodes.Ldloc_0).WithLabels(skip)
            );

            _ = il.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Ldloc, qualifiedId),
                new CodeInstruction(OpCodes.Ldloca, 2),
                new CodeInstruction(OpCodes.Call, typeof(RemoveQueuedFurniturePatches).GetMethod(nameof(CheckModifyFurnitureStack)))
            );

            _ = il.InsertAndAdvance(
                new CodeInstruction(OpCodes.Brfalse, skip),
                new CodeInstruction(OpCodes.Br, @break)
            );

            _ = il.MatchStartForward(
                new CodeMatch(OpCodes.Blt_S)
            );
            _ = il.Advance(1);
            _ = il.AddLabels([@break]);

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
