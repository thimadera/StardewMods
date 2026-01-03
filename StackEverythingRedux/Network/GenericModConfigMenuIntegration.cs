using StackEverythingRedux.Models;
using StardewModdingAPI;

namespace StackEverythingRedux.Network
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

            genericModConfigApi.AddNumberOption(
               mod,
               name: I18n.Config_MaxStackingNumber_Name,
               tooltip: I18n.Config_MaxStackingNumber_Tooltip,
               getValue: () => Config.MaxStackingNumber,
               setValue: v => Config.MaxStackingNumber = v
           );

            genericModConfigApi.AddSectionTitle(mod, () => "Stack Split Redux");

            genericModConfigApi.AddBoolOption(
                mod,
                name: I18n.Config_EnableStackSplitRedux_Name,
                tooltip: I18n.Config_EnableStackSplitRedux_Tooltip,
                getValue: () => Config.EnableStackSplitRedux,
                setValue: v => Config.EnableStackSplitRedux = v
            );

            genericModConfigApi.AddBoolOption(
                mod,
                name: I18n.Config_EnableTackleSplit_Name,
                tooltip: I18n.Config_EnableTackleSplit_Tooltip,
                getValue: () => Config.EnableTackleSplit,
                setValue: value => Config.EnableTackleSplit = value
            );

            genericModConfigApi.AddNumberOption(
                mod,
                name: I18n.Config_DefaultCraftingAmount_Name,
                tooltip: I18n.Config_DefaultCraftingAmount_Tooltip,
                getValue: () => Config.DefaultCraftingAmount,
                setValue: v => Config.DefaultCraftingAmount = v
            );

            genericModConfigApi.AddNumberOption(
                mod,
                name: I18n.Config_DefaultShopAmount_Name,
                tooltip: I18n.Config_DefaultShopAmount_Tooltip,
                getValue: () => Config.DefaultShopAmount,
                setValue: v => Config.DefaultShopAmount = v
            );

            genericModConfigApi.AddBoolOption(
                mod,
                name: I18n.Config_EnableStackEverything_Name,
                tooltip: I18n.Config_EnableStackEverything_Tooltip,
                getValue: () => Config.EnableStackEverything,
                setValue: v => Config.EnableStackEverything = v
            );
        }
    }
}
