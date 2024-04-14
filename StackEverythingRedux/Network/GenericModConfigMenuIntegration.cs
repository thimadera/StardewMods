using StardewModdingAPI;
using Thimadera.StardewMods.StackEverythingRedux.Models;

namespace Thimadera.StardewMods.StackEverythingRedux.Network
{
    internal class GenericModConfigMenuIntegration
    {
        public static void AddConfig(IGenericModConfigMenuApi genericModConfigApi, IManifest mod, IModHelper helper, ModConfig config)
        {

            if (genericModConfigApi is null)
            {
                Log.Trace("GMCM not available, skipping Mod Config Menu");
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

            genericModConfigApi.AddSectionTitle(mod, () => "Stack Split Redux");

            genericModConfigApi.AddBoolOption(
                mod,
                name: I18n.Config_EnableStackSplitRedux_Name,
                tooltip: I18n.Config_EnableStackSplitRedux_Tooltip,
                getValue: () => config.EnableStackSplitRedux,
                setValue: value => config.EnableStackSplitRedux = value
            );

            genericModConfigApi.AddNumberOption(
                mod,
                name: I18n.Config_DefaultCraftingAmount_Name,
                tooltip: I18n.Config_DefaultCraftingAmount_Tooltip,
                getValue: () => config.DefaultCraftingAmount,
                setValue: value => config.DefaultCraftingAmount = value
            );

            genericModConfigApi.AddNumberOption(
                mod,
                name: I18n.Config_DefaultShopAmount_Name,
                tooltip: I18n.Config_DefaultShopAmount_Tooltip,
                getValue: () => config.DefaultShopAmount,
                setValue: value => config.DefaultShopAmount = value
            );
        }
    }
}
