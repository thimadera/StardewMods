using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using System.Reflection;
using Thimadera.StardewMods.StackEverythingRedux.Network;
using Thimadera.StardewMods.StackEverythingRedux.Patches;
using SObject = StardewValley.Object;

namespace Thimadera.StardewMods.StackEverythingRedux
{
    internal class StackEverythingRedux : Mod
    {
        #region Internal Properties
        internal static Mod? Instance;
        internal static IManifest Manifest => Instance.ModManifest;
        internal static Harmony Harmony => new(Manifest.UniqueID);
        internal static IModHelper ModHelper => Instance.Helper;
        internal static ModConfig Config => ModHelper.ReadConfig<ModConfig>();
        internal static ITranslationHelper I18n => ModHelper.Translation;
        internal static IReflectionHelper Reflection => ModHelper.Reflection;
        internal static IInputHelper Input => ModHelper.Input;
        internal static IModEvents Events => ModHelper.Events;
        internal static IModRegistry Registry => ModHelper.ModRegistry;
        internal static StackSplit StackSplitRedux => new();
        #endregion

        public override void Entry(IModHelper helper)
        {
            Instance = this;

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            PatchHarmony();
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            GenericModConfigMenuIntegration.AddConfig();
        }

        private static void PatchHarmony()
        {
            Patch(nameof(SObject.maximumStackSize), typeof(Furniture), typeof(MaximumStackSizePatches));
            Patch(nameof(SObject.maximumStackSize), typeof(Wallpaper), typeof(MaximumStackSizePatches));
            Patch(nameof(SObject.maximumStackSize), typeof(SObject), typeof(MaximumStackSizePatches));

            Patch(nameof(Utility.tryToPlaceItem), typeof(Utility), typeof(TryToPlaceItemPatches));
            Patch(nameof(Item.canStackWith), typeof(Item), typeof(CanStackWithPatches));
            Patch(nameof(Tool.attach), typeof(Tool), typeof(AttachPatches));

            Patch("removeQueuedFurniture", typeof(GameLocation), typeof(RemoveQueuedFurniturePatches));
            Patch("doDoneFishing", typeof(FishingRod), typeof(DoDoneFishingPatches));
        }

        private static void Patch(string originalName, Type originalType, Type patchType)
        {
            BindingFlags originalSearch = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo? original = originalType.GetMethods(originalSearch).FirstOrDefault(m => m.Name == originalName);

            if (original == null)
            {
                Log.Error($"Failed to patch {originalType.Name}::{originalName}: could not find original method.");
                return;
            }

            MethodInfo[] patchMethods = patchType.GetMethods(BindingFlags.Static | BindingFlags.Public);
            MethodInfo? prefix = patchMethods.FirstOrDefault(m => m.Name == "Prefix");
            MethodInfo? postfix = patchMethods.FirstOrDefault(m => m.Name == "Postfix");

            if (prefix != null || postfix != null)
            {
                try
                {
                    _ = Harmony.Patch(original, prefix == null ? null : new HarmonyMethod(prefix), postfix == null ? null : new HarmonyMethod(postfix));
                    Log.Trace($"Patched {originalType}::{originalName} with{(prefix == null ? "" : $" {patchType.Name}::{prefix.Name}")}{(postfix == null ? "" : $" {patchType.Name}::{postfix.Name}")}");
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to patch {originalType.Name}::{originalName}: {e.Message}");
                }
            }
            else
            {
                Log.Error($"Failed to patch {originalType.Name}::{originalName}: both prefix and postfix are null.");
            }
        }
    }
}
