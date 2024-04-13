using GenericModConfigMenu;
using StardewModdingAPI;

namespace Thimadera.StardewMods.StackEverythingRedux.Network
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

            genericModConfigApi.AddNumberOption(
                mod,
                name: I18n.Config_MaxStackingNumber_Name,
                tooltip: I18n.Config_MaxStackingNumber_Tooltip,
                getValue: () => config.MaxStackingNumber,
                setValue: value => config.MaxStackingNumber = value
            );
        }
    }
}
