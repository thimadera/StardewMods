using System;
using RealClock.Models;
using StardewModdingAPI;

namespace RealClock.Network
{
    internal class GenericModConfigMenuIntegration
    {
        public static void AddConfig(IGenericModConfigMenuApi genericModConfigApi, IManifest mod, IModHelper helper, ModConfig Config)
        {

            if (genericModConfigApi is null)
            {
                return;
            }

            if (Config is null)
            {
                return;
            }

            I18n.Init(helper.Translation);

            genericModConfigApi.Register(
                mod,
                reset: () => Config = new ModConfig(),
                save: () => helper.WriteConfig(Config)
            );

            genericModConfigApi.AddBoolOption(
                mod,
                name: I18n.Config_TimeSpeedControl_Name,
                tooltip: I18n.Config_TimeSpeedControl_Tooltip,
                getValue: () => Config.TimeSpeedControl,
                setValue: v => Config.TimeSpeedControl = v
            );

            genericModConfigApi.AddNumberOption(
                mod,
                name: I18n.Config_SecondsToMinutes_Name,
                tooltip: I18n.Config_SecondsToMinutes_Tooltip,
                getValue: () => Config.SecondsToMinutes,
                setValue: v =>
                {
                    float sec = Math.Clamp(v, 0.05f, 20f);
                    Config.SecondsToMinutes = sec;
                }
            );

            genericModConfigApi.AddBoolOption(
                mod,
                name: I18n.Config_Show24Hours_Name,
                tooltip: I18n.Config_Show24Hours_Tooltip,
                getValue: () => Config.Show24Hours,
                setValue: v => Config.Show24Hours = v
            );
        }
    }
}
