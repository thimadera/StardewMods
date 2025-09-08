using HarmonyLib;
using StackEverythingRedux.Models;
using StackEverythingRedux.Network;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace StackEverythingRedux
{
    internal class StackEverythingRedux : Mod
    {
        internal static Mod Instance;
        internal static ModConfig Config;

        public override void Entry(IModHelper helper)
        {
            Instance = this;

            Config = helper.ReadConfig<ModConfig>();

            Log.Info($"StackEverythingRedux iniciado. MaxStack = {Config.MaxStackingNumber}");

            Harmony harmony = new(ModManifest.UniqueID);
            harmony.PatchAll();
            Log.Info("Harmony patches aplicados com sucesso.");

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            GenericModConfigMenuIntegration.AddConfig(
                Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu"),
                ModManifest,
                Helper,
                Config
            );
        }

    }
}
