using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using System.Reflection;
using Thimadera.StardewMods.StackEverythingRedux.Models;
using Thimadera.StardewMods.StackEverythingRedux.Network;
using Thimadera.StardewMods.StackEverythingRedux.Patches;
using Thimadera.StardewMods.StackEverythingRedux.Patches.Size;
using SObject = StardewValley.Object;

namespace Thimadera.StardewMods.StackEverythingRedux
{
    internal class StackEverythingRedux : Mod
    {
        /// <summary>
        /// These Convenience Properties are here so we don't have to keep passing a ref to Helper as params.
        /// </summary>
        #region Convenience Properties
        internal static Mod Instance;
        internal static ITranslationHelper I18n => Instance.Helper.Translation;
        internal static IReflectionHelper Reflection => Instance.Helper.Reflection;
        internal static IInputHelper Input => Instance.Helper.Input;
        internal static IModEvents Events => Instance.Helper.Events;
        internal static IModRegistry Registry => Instance.Helper.ModRegistry;
        internal static ModConfig Config;
        #endregion

        private static StackSplit StackSplitRedux;

        private Harmony harmony;

        public override void Entry(IModHelper helper)
        {
            harmony = new(ModManifest.UniqueID);
            Config = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            PatchStackEverythingMod();

            Instance = this;

            if (DetectConflict())
            {
                return;
            }

            Log.Info($"{ModManifest.UniqueID} version {typeof(StackEverythingRedux).Assembly.GetName().Version} (API version {API.Version}) is loading...");
            StackSplitRedux = new();
        }
        public override object GetApi()
        {
            return new API(StackSplitRedux);
        }

        public static bool DetectConflict()
        {
            bool conflict = false;
            foreach (string mID in StaticConfig.ConflictingMods)
            {
                if (Registry.IsLoaded(mID))
                {
                    Log.Alert($"{mID} detected!");
                    conflict = true;
                }
            }
            if (conflict)
            {
                Log.Error("Conflicting mods detected! Will abort loading this mod to prevent conflict/issues!");
                Log.Error("Please upload the log to https://smapi.io/log and tell pepoluan about this!");
            }
            return conflict;
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

        private void PatchStackEverythingMod()
        {
            IDictionary<string, Type> patchedTypeReplacements = new Dictionary<string, Type>
            {
                [nameof(SObject.maximumStackSize)] = typeof(MaximumStackSizePatch),
            };

            IList<Type> typesToPatch = [typeof(Furniture), typeof(Wallpaper), typeof(SObject)];

            foreach (Type type in typesToPatch)
            {
                foreach (KeyValuePair<string, Type> replacement in patchedTypeReplacements)
                {
                    Patch(harmony, replacement.Key, type, BindingFlags.Instance | BindingFlags.Public, replacement.Value);
                }
            }

            IDictionary<string, Tuple<Type, Type>> otherReplacements = new Dictionary<string, Tuple<Type, Type>>()
            {
                {"removeQueuedFurniture", new Tuple<Type, Type>(typeof(GameLocation), typeof(FurniturePickupPatch))},
                {nameof(Utility.tryToPlaceItem), new Tuple<Type, Type>(typeof(Utility), typeof(TryToPlaceItemPatch))},
                {"doDoneFishing", new Tuple<Type, Type>(typeof(FishingRod), typeof(DoDoneFishingPatch))},
                {nameof(Item.canStackWith), new Tuple<Type, Type>(typeof(Item), typeof(CanStackWithPatch))},
                {nameof(Tool.attach), new Tuple<Type, Type>(typeof(Tool), typeof(AttachPatch))}
            };

            foreach (KeyValuePair<string, Tuple<Type, Type>> replacement in otherReplacements)
            {
                Patch(harmony, replacement.Key, replacement.Value.Item1, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, replacement.Value.Item2);
            }
        }

        private void Patch(Harmony harmony, string originalName, Type originalType, BindingFlags originalSearch, Type patchType)
        {
            if (originalType == null)
            {
                throw new ArgumentException("Original type can't be null.");
            }

            if (patchType == null)
            {
                throw new ArgumentException("Patch type can't be null.");
            }

            MethodInfo original = originalType.GetMethods(originalSearch).FirstOrDefault(m => m.Name == originalName);

            if (original == null)
            {
                Monitor.Log($"Failed to patch {originalType.Name}::{originalName}: could not find original method.", LogLevel.Error);
                return;
            }

            MethodInfo[] patchMethods = patchType.GetMethods(BindingFlags.Static | BindingFlags.Public);
            MethodInfo prefix = patchMethods.FirstOrDefault(m => m.Name == "Prefix");
            MethodInfo postfix = patchMethods.FirstOrDefault(m => m.Name == "Postfix");

            if (prefix == null && postfix == null)
            {
                Monitor.Log($"Failed to patch {originalType.Name}::{originalName}: both prefix and postfix are null.", LogLevel.Error);
            }
            else
            {
                try
                {
                    _ = harmony.Patch(original, prefix == null ? null : new HarmonyMethod(prefix), postfix == null ? null : new HarmonyMethod(postfix));
                    Monitor.Log($"Patched {originalType}::{originalName} with{(prefix == null ? "" : $" {patchType.Name}::{prefix.Name}")}{(postfix == null ? "" : $" {patchType.Name}::{postfix.Name}")}", LogLevel.Trace);
                }
                catch (Exception e)
                {
                    Monitor.Log($"Failed to patch {originalType.Name}::{originalName}: {e.Message}", LogLevel.Error);
                }
            }
        }
    }
}
