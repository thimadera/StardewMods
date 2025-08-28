using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using StackEverythingRedux.UI;
using StardewValley;
using StardewValley.Menus;
using SFarmer = StardewValley.Farmer;
using SObject = StardewValley.Object;

namespace StackEverythingRedux.MenuHandlers
{
    public class ItemGrabMenuHandler : BaseMenuHandler<ItemGrabMenu>
    {
        /// <summary>Native player inventory menu.</summary>
        private InventoryMenu PlayerInventoryMenu = null;

        /// <summary>Native shop inventory menu.</summary>
        private InventoryMenu ItemsToGrabMenu = null;

        /// <summary>If the callbacks have been hooked yet so we don't do it unnecessarily.</summary>
        private bool CallbacksHooked = false;

        /// <summary>Native item select callback.</summary>
        private ItemGrabMenu.behaviorOnItemSelect OriginalItemSelectCallback;

        /// <summary>Native item grab callback.</summary>
        private ItemGrabMenu.behaviorOnItemSelect OriginalItemGrabCallback;

        /// <summary>The item being hovered when the split menu is opened.</summary>
        private Item HoverItem = null;

        /// <summary>The amount we wish to buy/sell.</summary>
        private int StackAmount = 0;

        /// <summary>The total number of items in the hovered stack.</summary>
        private int TotalItems = 0;

        /// <summary>The currently held item (in the Native Menu).</summary>
        private Item HeldItem => NativeMenu.heldItem;


        /// <summary>Null constructor.</summary>
        public ItemGrabMenuHandler()
            : base()
        {
            // We're handling the inventory in such a way that we don't need the generic handler.
            HasInventory = false;
        }

        /// <summary>Allows derived handlers to provide additional checks before opening the split menu.</summary>
        /// <returns>True if it can be opened.</returns>
        protected override bool CanOpenSplitMenu()
        {
            bool canOpen = NativeMenu.allowRightClick;
            return canOpen && base.CanOpenSplitMenu();
        }

        /// <summary>Tells the handler to close the split menu.</summary>
        public override void CloseSplitMenu()
        {
            base.CloseSplitMenu();

            if (CallbacksHooked)
            {
                Log.Error($"[{nameof(ItemGrabMenuHandler)}.{nameof(CloseSplitMenu)}] Callbacks shouldn't still be hooked on closing!");
            }
        }

        /// <summary>Called when the current handler loses focus when the split menu is open, allowing it to cancel the operation or run the default behaviour.</summary>
        /// <returns>If the input was handled or consumed.</returns>
        protected override EInputHandled CancelMove()
        {
            // Not hovering above anything so pass-through (??)
            if (HoverItem is null)
            {
                return EInputHandled.NotHandled;
            }

            // If being cancelled from a click else-where then the keyboad state won't have shift held (unless they're still holding it),
            // in which case the default right-click behavior will run and only a single item will get moved instead of half the stack.
            // Therefore we must make sure it's still using our callback so we can correct the amount.
            _ = HookCallbacks();

            // Run the regular command
            NativeMenu?.receiveRightClick(ClickItemLocation.X, ClickItemLocation.Y);

            CloseSplitMenu();

            // Consume input so the menu doesn't run left click logic as well
            return EInputHandled.Consumed;
        }

        /// <summary>Main event that derived handlers use to setup necessary hooks and other things needed to take over how the stack is split.</summary>
        /// <returns>If the input was handled or consumed.</returns>
        protected override EInputHandled OpenSplitMenu()
        {
            try
            {
                PlayerInventoryMenu = NativeMenu.inventory;
                ItemsToGrabMenu = NativeMenu.ItemsToGrabMenu;

                // Emulate the right click method that would normally happen (??)
                HoverItem = NativeMenu.hoveredItem;
            }
            catch (Exception e)
            {
                Log.Error($"[{nameof(ItemGrabMenuHandler)}.{nameof(OpenSplitMenu)}] Had an exception:\n{e}");
                return EInputHandled.NotHandled;
            }

            // Do nothing if we're not hovering over an item, or item is single (no point in splitting)
            if (HoverItem == null || HoverItem.Stack <= 1)
            {
                return EInputHandled.NotHandled;
            }

            TotalItems = HoverItem.Stack;
            // +1 before /2 ensures number is rounded UP
            StackAmount = (TotalItems + 1) / 2; // default at half

            // Create the split menu
            SplitMenu = new StackSplitMenu(OnStackAmountReceived, StackAmount);

            return EInputHandled.Consumed;
        }

        /// <summary>Callback given to the split menu that is invoked when a value is submitted.</summary>
        /// <param name="s">The user input.</param>
        protected override void OnStackAmountReceived(string s)
        {
            // Store amount
            if (int.TryParse(s, out StackAmount))
            {
                if (StackAmount > 0)
                {
                    if (!HookCallbacks())
                    {
                        throw new Exception("Failed to hook callbacks");
                    }

                    NativeMenu.receiveRightClick(ClickItemLocation.X, ClickItemLocation.Y);
                }
                else
                {
                    RevertItems();
                }
            }

            base.OnStackAmountReceived(s);
        }

        /// <summary>Callback override for when an item in the inventory is selected.</summary>
        /// <param name="item">Item that was selected.</param>
        /// <param name="who">The player that selected it.</param>
        private void OnItemSelect(Item item, SFarmer who)
        {
            MoveItems(item, who, PlayerInventoryMenu, OriginalItemSelectCallback);
        }

        /// <summary>Callback override for when an item in the shop is selected.</summary>
        /// <param name="item">Item that was selected.</param>
        /// <param name="who">The player that selected it.</param>
        private void OnItemGrab(Item item, SFarmer who)
        {
            MoveItems(item, who, ItemsToGrabMenu, OriginalItemGrabCallback);
        }

        /// <summary>Cancels the operation so no items are sold or bought.</summary>
        private void RevertItems()
        {
            if (HoverItem != null && TotalItems > 0)
            {
                Log.Trace($"[{nameof(ItemGrabMenuHandler)}.{nameof(RevertItems)}] Reverting items");
                HoverItem.Stack = TotalItems;

                RestoreNativeCallbacks();
            }
        }

        /// <summary>Replaces the native shop callbacks with our own so we can intercept the operation to modify the amount.</summary>
        /// <returns>If it was hooked successfully.</returns>
        private bool HookCallbacks()
        {
            if (CallbacksHooked)
            {
                return true;
            }

            try
            {
                // Replace the delegates with our own
                StardewModdingAPI.IReflectedField<ItemGrabMenu.behaviorOnItemSelect> itemSelectCallbackField = StackEverythingRedux.Reflection.GetField<ItemGrabMenu.behaviorOnItemSelect>(NativeMenu, "behaviorFunction");
                //var itemGrabCallbackField = typeof(ItemGrabMenu).GetField("behaviorOnItemGrab");

                OriginalItemGrabCallback = NativeMenu.behaviorOnItemGrab;
                OriginalItemSelectCallback = itemSelectCallbackField.GetValue();

                NativeMenu.behaviorOnItemGrab = new ItemGrabMenu.behaviorOnItemSelect(OnItemGrab);
                itemSelectCallbackField.SetValue(OnItemSelect);

                CallbacksHooked = true;
            }
            catch (Exception e)
            {
                Log.Error($"[{nameof(ItemGrabMenuHandler)}.{nameof(HookCallbacks)}] Failed to hook ItemGrabMenu callbacks:\n{e}");
                return false;
            }
            return true;
        }

        /// <summary>Sets the callbacks back to the native ones.</summary>
        private void RestoreNativeCallbacks()
        {
            if (!CallbacksHooked)
            {
                return;
            }

            try
            {
                StardewModdingAPI.IReflectedField<ItemGrabMenu.behaviorOnItemSelect> itemSelectCallbackField = StackEverythingRedux.Reflection.GetField<ItemGrabMenu.behaviorOnItemSelect>(NativeMenu, "behaviorFunction");
                //var itemGrabCallbackField = typeof(ItemGrabMenu).GetField("behaviorOnItemGrab");

                itemSelectCallbackField.SetValue(OriginalItemSelectCallback);
                NativeMenu.behaviorOnItemGrab = OriginalItemGrabCallback;

                CallbacksHooked = false;
            }
            catch (Exception e)
            {
                Log.Error($"[{nameof(ItemGrabMenuHandler)}.{nameof(RestoreNativeCallbacks)}] Failed to restore native callbacks:\n{e}");
            }
        }

        private struct Snap
        {
            public string Qid;
            public int Stack;
            public float Pool;
        }
        private static List<Snap> Snapshot(IList<Item> inv)
        {
            List<Snap> list = new(inv?.Count ?? 0);
            if (inv == null)
            {
                return list;
            }

            for (int i = 0; i < inv.Count; i++)
            {
                if (inv[i] is SObject o)
                {
                    list.Add(new Snap
                    {
                        Qid = o.QualifiedItemId,
                        Stack = o.Stack,
                        Pool = DurPool.ReadPool(o)
                    });
                }
                else
                {
                    list.Add(new Snap { Qid = null, Stack = 0, Pool = 0f });
                }
            }
            return list;
        }

        private static class DurPool
        {
            private const string KeyPool = "ser.durPool";
            private const string KeyMax = "ser.maxUses";
            public static bool IsTackle(Item it)
            {
                return it is SObject o && o.Category == -22;
            }

            public static void NormalizeStack(SObject rep)
            {
                float p = ReadPool(rep);
                int whole = (int)Math.Floor(p);
                float frac = p - whole;
                int newStack = whole + (frac > 0f ? 1 : 0);
                if (newStack == 0)
                {
                    newStack = 1;
                }

                rep.Stack = newStack;

                int max = MaxUses(rep);
                int uses = (frac > 0f) ? (int)Math.Round(frac * max, MidpointRounding.AwayFromZero) : max;
                SetUsesLeft(rep, uses);

                WritePool(rep, Math.Clamp(p, 0f, rep.Stack));
            }

            public static float ReadPool(SObject rep)
            {
                if (rep.modData.TryGetValue(KeyPool, out string s) && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                {
                    return Math.Clamp(f, 0f, rep.Stack);
                }

                float one = RemFraction(rep);
                float pool = one + Math.Max(0, rep.Stack - 1);
                return Math.Clamp(pool, 0f, rep.Stack);
            }
            public static void WritePool(SObject rep, float pool)
            {
                pool = Math.Clamp(pool, 0f, rep.Stack);
                rep.modData[KeyPool] = pool.ToString(CultureInfo.InvariantCulture);
            }
            public static int MaxUses(SObject o)
            {
                if (o.modData.TryGetValue(KeyMax, out string s) && int.TryParse(s, out int m))
                {
                    return m;
                }

                int def = 20; o.modData[KeyMax] = def.ToString(CultureInfo.InvariantCulture); return def;
            }
            public static int UsesLeft(SObject o)
            {
                try { return o.uses.Value; } catch { return MaxUses(o); }
            }
            public static void SetUsesLeft(SObject o, int uses)
            {
                try { o.uses.Value = uses; } catch { }
            }
            public static float RemFraction(SObject o)
            {
                int max = MaxUses(o); int left = UsesLeft(o);
                return max <= 0 ? 1f : Math.Clamp(left / (float)max, 0f, 1f);
            }

            public static float MaterializeOne(SObject fromStack, SObject singleOut)
            {
                float pool = ReadPool(fromStack);
                int max = MaxUses(fromStack);
                float takeFrac;
                if (pool >= 1f) { takeFrac = 1f; pool -= 1f; SetUsesLeft(singleOut, max); }
                else { takeFrac = pool; pool = 0f; SetUsesLeft(singleOut, (int)Math.Round(takeFrac * max, MidpointRounding.AwayFromZero)); }
                WritePool(fromStack, pool);
                return takeFrac;
            }
        }

        /// <summary>Updates the number of items being held by the player based on what was input to the split menu.</summary>
        /// <param name="item">The selected item.</param>
        /// <param name="who">The player that selected the items.</param>
        /// <param name="inventoryMenu">Either the player inventory or the shop inventory.</param>
        /// <param name="callback">The native callback to invoke to continue with the regular behavior after we've modified the stack.</param>
        private void MoveItems(Item item, SFarmer who, InventoryMenu inventoryMenu, ItemGrabMenu.behaviorOnItemSelect callback)
        {
            Debug.Assert(StackAmount > 0);

            IList<Item> srcInv = inventoryMenu?.actualInventory;
            IList<Item> dstInv = (inventoryMenu == PlayerInventoryMenu)
                ? ItemsToGrabMenu?.actualInventory
                : PlayerInventoryMenu?.actualInventory;

            List<Snap> preSrc = Snapshot(srcInv);
            List<Snap> preDst = Snapshot(dstInv);

            Item heldItem = HeldItem;
            if (heldItem != null)
            {
                int wantToHold = Math.Min(TotalItems, StackAmount);

                HoverItem.Stack = TotalItems - wantToHold;
                heldItem.Stack = wantToHold;
                item.Stack = wantToHold;

                if (HoverItem.Stack <= 0)
                {
                    int index = inventoryMenu.actualInventory.IndexOf(HoverItem);
                    if (index > -1)
                    {
                        inventoryMenu.actualInventory[index] = null;
                    }
                }

                float movedPool = 0f;

                if (HoverItem is SObject srcObj && DurPool.IsTackle(srcObj) && heldItem is SObject heldObj)
                {
                    if (wantToHold == 1)
                    {
                        movedPool = DurPool.MaterializeOne(srcObj, heldObj);
                        DurPool.NormalizeStack(srcObj);
                        Log.Debug($"[ItemGrab] split tackle x1: movedPool={movedPool:0.###} (materialized), srcPoolNow={DurPool.ReadPool(srcObj):0.###}");
                        DurPool.WritePool(heldObj, 0f);
                    }
                    else
                    {
                        float srcPool = DurPool.ReadPool(srcObj);
                        float frac = wantToHold / (float)TotalItems;
                        float alloc = Math.Min(wantToHold, srcPool * frac);
                        movedPool = alloc;

                        DurPool.WritePool(srcObj, Math.Max(0f, srcPool - alloc));
                        DurPool.WritePool(heldObj, alloc);

                        Log.Debug($"[ItemGrab] split tackle x{wantToHold}: allocPool={alloc:0.###} from srcPool={srcPool:0.###} → srcPoolNow={DurPool.ReadPool(srcObj):0.###}");
                    }
                }
                RestoreNativeCallbacks();

                callback?.Invoke(item, who);

                try
                {
                    if (movedPool > 0f && dstInv != null)
                    {
                        List<Snap> postDst = Snapshot(dstInv);
                        string movedQid = (heldItem as SObject)?.QualifiedItemId ?? (item as SObject)?.QualifiedItemId;

                        int chosen = -1;
                        for (int i = 0; i < postDst.Count; i++)
                        {
                            Snap before = preDst.Count > i ? preDst[i] : new Snap();
                            Snap after = postDst[i];

                            if (after.Qid == movedQid && after.Stack > before.Stack)
                            {
                                chosen = i;
                                break;
                            }
                        }

                        if (chosen >= 0 && dstInv[chosen] is SObject dstObj)
                        {
                            float dstPool = DurPool.ReadPool(dstObj);
                            DurPool.WritePool(dstObj, dstPool + movedPool);
                            DurPool.NormalizeStack(dstObj);
                            Log.Debug($"[ItemGrab] dest normalized: pool={DurPool.ReadPool(dstObj):0.###}, stack={dstObj.Stack}, usesRep={DurPool.UsesLeft(dstObj)}");
                        }
                        else
                        {
                            Log.Trace("[ItemGrab] no destination slot change detected for pool add (maybe kept in hand).");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"[ItemGrab] post-callback pool merge failed: {ex}");
                }

                return;
            }

            RestoreNativeCallbacks();
            callback?.Invoke(item, who);
        }
    }
}
