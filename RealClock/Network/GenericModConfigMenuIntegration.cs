using RealClock.Models;
using StardewModdingAPI;

namespace RealClock.Network
{
    internal class GenericModConfigMenuIntegration
    {
        public static void AddConfig(IGenericModConfigMenuApi genericModConfigApi, IManifest mod, IModHelper helper, ModConfig config)
        {

            if (genericModConfigApi is null)
            {
                return;
            }

            if (config is null)
            {
                return;
            }

            I18n.Init(helper.Translation);

            genericModConfigApi.Register(
                mod,
                reset: () => config = new ModConfig(),
                save: () => helper.WriteConfig(config)
            );

            genericModConfigApi.AddBoolOption(
                mod,
                name: I18n.Config_Enabled_Name,
                tooltip: I18n.Config_Enabled_Tooltip,
                getValue: () => config.Enabled,
                setValue: value => config.Enabled = value
            );

            genericModConfigApi.AddNumberOption(
                mod,
                name: I18n.Config_SecondsToMinutes_Name,
                tooltip: I18n.Config_SecondsToMinutes_Tooltip,
                getValue: () => config.SecondsToMinutes,
                setValue: value => config.SecondsToMinutes = value
            );

            genericModConfigApi.AddBoolOption(
                mod,
                name: I18n.Config_Show24Hours_Name,
                tooltip: I18n.Config_Show24Hours_Tooltip,
                getValue: () => config.Show24Hours,
                setValue: value => config.Show24Hours = value
            );
        }
    }
}
