using GenericModConfigMenu;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using Thimadera.StardewMods.RealClock.Patching;

namespace Thimadera.StardewMods.RealClock
{
    internal sealed class ModEntry : Mod
    {
        private ModConfig Config;

        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<ModConfig>();
            Game1Patches.Config = this.Config;
            DayTimeMoneyBoxPatches.Config = this.Config;

            Harmony harmony = new(this.ModManifest.UniqueID);

            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), nameof(Game1.UpdateGameClock)),
                prefix: new HarmonyMethod(typeof(Game1Patches), nameof(Game1Patches.UpdateGameClock))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(DayTimeMoneyBox), "draw", new Type[] { typeof(SpriteBatch) }, null),
                prefix: new HarmonyMethod(typeof(DayTimeMoneyBoxPatches), nameof(DayTimeMoneyBoxPatches.draw))
            );

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
                "spacechase0.GenericModConfigMenu"
            );

            if (configMenu is null)
                return;

            if (this.Config is null)
                return;

            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Mod Enabled",
                getValue: () => this.Config.Enabled,
                setValue: value => this.Config.Enabled = value
            );

            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "Seconds to Minutes",
                tooltip: () => "In how many seconds should the clock change",
                getValue: () => this.Config.SecondsToMinutes,
                setValue: value => this.Config.SecondsToMinutes = value
            );
        }
    }
}
