using GenericModConfigMenu;
using StardewModdingAPI;

namespace Thimadera.StardewMods.RealClock.Network
{
    internal class GenericModConfigMenuIntegration
    {
        static public void AddConfig(IGenericModConfigMenuApi genericModConfigApi, IManifest mod, IModHelper helper, ModConfig config)
        {

            if (genericModConfigApi is null)
                return;

            if (config is null)
                return;

            genericModConfigApi.Register(
                mod,
                reset: () => config = new ModConfig(),
                save: () => helper.WriteConfig(config)
            );

            genericModConfigApi.AddBoolOption(
                mod,
                name: () => "Mod Enabled",
                getValue: () => config.Enabled,
                setValue: value => config.Enabled = value
            );

            genericModConfigApi.AddNumberOption(
                mod,
                name: () => "Seconds to Minutes",
                tooltip: () => "In how many seconds should the clock change",
                getValue: () => config.SecondsToMinutes,
                setValue: value => config.SecondsToMinutes = value
            );

            genericModConfigApi.AddBoolOption(
                mod,
                name: () => "24 hours mode",
                getValue: () => config.Show24Hours,
                setValue: value => config.Show24Hours = value
            );
        }
    }
}
