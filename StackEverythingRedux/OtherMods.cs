using Thimadera.StardewMods.StackEverythingRedux.MenuHandlers;

namespace Thimadera.StardewMods.StackEverythingRedux
{
    internal static class OtherMods
    {
        //                         mod_ID    menu_class_name  type_of_handler
        private static readonly Dictionary<string, List<Tuple<string, Type>>> MenuAndHandlerByMods = [];
        private static void Add(string modUniqueID, params object[] args)
        {
            List<Tuple<string, Type>> lst = [];
            for (int i = 0; i < args.Length; i += 2)
            {
                lst.Add(new Tuple<string, Type>((string)args[i], (Type)args[i + 1]));
            }

            MenuAndHandlerByMods[modUniqueID] = lst;
        }
        static OtherMods()
        {
            Add(
                // Other Mod's UniqueID
                "CJBok.ItemSpawner",
                // Pairwise arg: Menu classes to intercept, suitable handler
                "CJBItemSpawner.Framework.ItemMenu", typeof(ItemGrabMenuHandler)
                );
            // Repeat calls to Add() as necessary
        }
        public static IEnumerable<KeyValuePair<string, List<Tuple<string, Type>>>> AsEnumerable()
        {
            return MenuAndHandlerByMods.AsEnumerable();
        }
    }
}
