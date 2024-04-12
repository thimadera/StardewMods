using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
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
        public static readonly Type[] PatchedTypes = [typeof(Furniture), typeof(Wallpaper)];
        private readonly ICopier<Furniture> furnitureCopier = new FurnitureCopier();
        private bool isInDecoratableLocation;

        private IList<Furniture> lastKnownFurniture;

        public override void Entry(IModHelper helper)
        {
            lastKnownFurniture = [];
            Harmony harmony = new(ModManifest.UniqueID);

            IDictionary<string, Type> patchedTypeReplacements = new Dictionary<string, Type>
            {
                [nameof(StardewValley.Object.maximumStackSize)] = typeof(MaximumStackSizePatch),
                [nameof(StardewValley.Object.drawInMenu)] = typeof(DrawInMenuPatch)
            };

            IList<Type> typesToPatch = [.. PatchedTypes];

            foreach (Type type in typesToPatch)
            {
                foreach (KeyValuePair<string, Type> replacement in patchedTypeReplacements)
                {
                    Patch(harmony, replacement.Key, type, BindingFlags.Instance | BindingFlags.Public, replacement.Value);
                }
            }

            Patch(harmony, nameof(Item.addToStack), typeof(Item), BindingFlags.Instance | BindingFlags.Public, typeof(AddToStackPatch));

            if (helper.ModRegistry.IsLoaded("Platonymous.CustomFurniture"))
            {
                try
                {
                    Patch(harmony, nameof(StardewValley.Object.drawInMenu), Type.GetType("CustomFurniture.CustomFurniture, CustomFurniture"), BindingFlags.Instance | BindingFlags.Public, typeof(DrawInMenuPatch));
                }
                catch (Exception e)
                {
                    Monitor.Log("Failed to add support for Custom Furniture.");
                    Monitor.Log(e.ToString());
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

            Patch(harmony, nameof(StardewValley.Object.maximumStackSize), typeof(StardewValley.Object), BindingFlags.Instance | BindingFlags.Public, typeof(MaximumStackSizePatch));

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
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

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (e.IsMultipleOf(15))
            {
                bool wasInDecoratableLocation = isInDecoratableLocation;

                if (Game1.currentLocation is not DecoratableLocation decLoc)
                {
                    isInDecoratableLocation = false;
                    return;
                }

                isInDecoratableLocation = true;

                if (wasInDecoratableLocation)
                {
                    for (int i = 0; i < decLoc.furniture.Count; i++)
                    {
                        Furniture f = decLoc.furniture[i];
                        if (!lastKnownFurniture.Contains(f) && Game1.player.Items.Contains(f))
                        {
                            Furniture copy = furnitureCopier.Copy(f);
                            if (copy != null)
                            {
                                decLoc.furniture[i] = copy;

                                copy.TileLocation = f.TileLocation;
                                copy.boundingBox.Value = f.boundingBox.Value;
                                copy.defaultBoundingBox.Value = f.defaultBoundingBox.Value;
                                copy.updateDrawPosition();
                            }
                            else
                            {
                                Monitor.Log($"Failed to make copy of furniture: {f.Name} - {f.GetType().Name}.", LogLevel.Error);
                            }
                        }
                    }
                }

                lastKnownFurniture.Clear();
                foreach (Furniture f in decLoc.furniture)
                {
                    lastKnownFurniture.Add(f);
                }
            }
        }
    }
}
