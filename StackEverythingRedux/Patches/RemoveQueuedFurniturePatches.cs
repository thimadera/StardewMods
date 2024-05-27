using HarmonyLib;
using StardewValley;
using StardewValley.Inventories;
using System.Reflection.Emit;

namespace StackEverythingRedux.Patches
{
    internal class RemoveQueuedFurniturePatches
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source, ILGenerator gen)
        {
            CodeMatcher il = new CodeMatcher(source, gen);
            LocalBuilder qualifiedId = gen.DeclareLocal(typeof(string));
            LocalBuilder item = gen.DeclareLocal(typeof(Item));
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

            // item = who.Items[i];
            il.InsertAndAdvance(
                new CodeInstruction(OpCodes.Callvirt, typeof(Farmer).GetProperty(nameof(Farmer.Items)).GetMethod),
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Callvirt, typeof(Inventory).GetMethod("get_Item")),
                new CodeInstruction(OpCodes.Stloc, item)
            );

            // if item != null
            il.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc, item),
                new CodeInstruction(OpCodes.Brfalse, skip)
            );

            // && item.QualifiedItemId == qualifiedId
            il.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc, item),
                new CodeInstruction(OpCodes.Callvirt, typeof(Item).GetProperty(nameof(Item.QualifiedItemId)).GetMethod),
                new CodeInstruction(OpCodes.Ldloc, qualifiedId),
                new CodeInstruction(OpCodes.Callvirt, typeof(string).GetMethod(nameof(string.Equals), [typeof(string)])),
                new CodeInstruction(OpCodes.Brfalse, skip)
            );

            // item[i].Stack++
            il.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc, item),
                new CodeInstruction(OpCodes.Ldloc, item),
                new CodeInstruction(OpCodes.Callvirt, typeof(Item).GetProperty(nameof(Item.Stack)).GetMethod),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Callvirt, typeof(Item).GetProperty(nameof(Item.Stack)).SetMethod)
            );

            // who.CurrentToolIndex = i;
            il.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Callvirt, typeof(Farmer).GetProperty(nameof(Farmer.CurrentToolIndex)).SetMethod)
            );

            // foundInToolbar = true;
            il.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Stloc_2)
            );

            // break;
            il.InsertAndAdvance(
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
    }
}
