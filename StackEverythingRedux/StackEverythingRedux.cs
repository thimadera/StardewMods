using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using System.Reflection;
using Thimadera.StardewMods.StackEverythingRedux.ObjectCopiers;
using Thimadera.StardewMods.StackEverythingRedux.Patches;
using Thimadera.StardewMods.StackEverythingRedux.Patches.Size;

namespace Thimadera.StardewMods.StackEverythingRedux
{
    public class StackEverythingRedux : Mod
    {
        public static readonly Type[] PatchedTypes = [typeof(Furniture), typeof(Wallpaper), typeof(StardewValley.Object)];
        private readonly ICopier<Furniture> furnitureCopier = new FurnitureCopier();
        private readonly bool isInDecoratableLocation;

        private IList<Furniture> lastKnownFurniture;

        public override void Entry(IModHelper helper)
        {
            lastKnownFurniture = [];
            Harmony harmony = new(ModManifest.UniqueID);

            IDictionary<string, Type> patchedTypeReplacements = new Dictionary<string, Type>
            {
                [nameof(StardewValley.Object.maximumStackSize)] = typeof(MaximumStackSizePatch),
            };

            IList<Type> typesToPatch = [.. PatchedTypes];

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
                {nameof(Item.canStackWith), new Tuple<Type, Type>(typeof(Item), typeof(CanStackWithPatch))}
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
