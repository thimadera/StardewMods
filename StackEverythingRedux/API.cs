using Thimadera.StardewMods.StackEverythingRedux.MenuHandlers;

namespace Thimadera.StardewMods.StackEverythingRedux
{
    public class API : IStackSplitAPI
    {
        public static readonly string Version = "0.9.0";

        internal static StackSplit StackSplitRedux;

        public API(StackSplit mod)
        {
            StackSplitRedux = mod;
        }

        public bool TryRegisterMenu(Type menuType)
        {
            if (HandlerMapping.TryGetHandler(menuType, out IMenuHandler handler))
            {
                HandlerMapping.Add(menuType, handler);
                Log.Debug($"API: Registered {menuType}, handled by {handler.GetType()}");
                return true;
            }
            Log.Error($"API: Don't know how to handle {menuType}, not registered!");
            return false;
        }
    }
}
