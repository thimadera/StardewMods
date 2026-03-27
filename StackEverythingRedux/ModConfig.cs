using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace StackEverythingRedux
{
    public class ModConfig
    {
        public bool EnableStackSplitRedux { get; set; } = true;
        public int MaxStackingNumber { get; set; } = 999;
        public int DefaultCraftingAmount { get; set; } = 1;
        public int DefaultShopAmount { get; set; } = 5;
        public bool EnableTackleSplit { get; set; } = true;
        public bool DebuggingMode { get; set; } = false;
    }

    /// <summary>
    /// This class containe "tunables" that should not be user-editable
    /// </summary>
    /// <remarks>Gathered here so we can tune the mod from one place, instead of hunting down config knobs everywhere</remarks>
    internal static class StaticConfig
    {
        /// <summary>Valid modifier keys to hold while RightClick-ing</summary>
        internal static readonly SButton[] ModifierKeys = [SButton.LeftShift, SButton.RightShift];

        /// <summary>Delay between new menu appearing & our handler beginning</summary>
        /// <remarks>To allow time for other mods to manipulate inventories</remarks>
        internal static readonly int SplitMenuOpenDelayTicks = 2;

        /// <summary>Text color when the text is highlighted. This should contrast with HighlightColor.</summary>
        internal static readonly Color HighlightTextColor = Color.White;

        /// <summary>The background color of the highlighted text.</summary>
        internal static readonly Color HighlightColor = Color.Blue;

        /// <summary>Items that should not stack.</summary>
        internal static HashSet<string> NoStackQualifiedIds { get; set; } =
        [
            // Free Cactus
            "(F)FreeCactus",

            // Trinkets
            "(TR)BasiliskPaw",
            "(TR)FairyBox",
            "(TR)FrogEgg",
            "(TR)GoldenSpur",
            "(TR)IceRod",
            "(TR)MagicHairGel",
            "(TR)MagicQuiver",
            "(TR)ParrotEgg",

            // Weapons
            "(W)4", // Galaxy Sword
            "(W)23", // Galaxy Dagger
            "(W)29", // Galaxy Hammer
            "(W)61", // Iridium Needle
            "(W)62", // Infinity Blade
            "(W)63", // Infinity Gavel
            "(W)64", // Infinity Dagger
            "(W)65", // Meowmere
            "(W)66",  // Iridium Scythe

            // Forged rings
            "(O)880"
        ];
    }
}
