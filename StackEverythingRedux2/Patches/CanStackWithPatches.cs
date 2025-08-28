using StardewValley;
using StardewValley.Objects;

namespace StackEverythingRedux.Patches
{
    internal class CanStackWithPatches
    {
        private static string Desc(ISalable s)
        {
            if (s is null)
            {
                return "null";
            }

            if (s is Item it)
            {
                string qid = (it as Object)?.QualifiedItemId ?? it.QualifiedItemId;
                int cat = (it as Object)?.Category ?? 0;
                return $"{it.DisplayName ?? it.Name} qid={qid} cat={cat} stack={it.Stack}";
            }
            return s.GetType().Name;
        }

        public static bool Prefix(Item __instance, ref bool __result, ISalable other)
        {
            Log.Debug($"[canStackWith/Prefix] self={Desc(__instance)} | other={Desc(other)}");

            if ((__instance is StorageFurniture d1 && d1.heldItems.Count != 0)
                || (other is StorageFurniture d2 && d2.heldItems.Count != 0))
            {
                __result = false;
                Log.Debug("[canStackWith] blocked: StorageFurniture with contents.");
                return false;
            }

            if (other is not Item otherItem)
            {
                Log.Trace("[canStackWith] other is not Item → vanilla.");
                return true;
            }

            if (__instance is Object a && otherItem is Object b)
            {
                if (a.Category == -22 && b.Category == -22 && a.QualifiedItemId == b.QualifiedItemId)
                {
                    __result = true;
                    Log.Debug("[canStackWith] force-allow: same tackle type (qid match).");
                    return false;
                }
            }

            Log.Trace("[canStackWith] fall-through → vanilla.");
            return true;
        }
    }
}
