using System;
using System.Collections.Generic;
using System.Globalization;
using StardewValley;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace StackEverythingRedux.MenuHandlers.GameMenuHandlers
{
    public class InventoryPageHandler : GameMenuPageHandler<InventoryPage>
    {
        private GameMenu _root;
        private InventoryPage _page;
        private InventoryHandler _invHandler;

        private struct Snap { public string Qid; public int Stack; public float Pool; }
        private readonly Dictionary<int, Snap> _before = [];

        private InventoryMenu InvMenu => _page?.inventory;
        private IList<Item> Inv => InvMenu?.actualInventory;

        public InventoryPageHandler() : base() { }

        public bool Open(GameMenu menu, IClickableMenu page, InventoryHandler invHandler)
        {
            _root = menu;
            _page = page as InventoryPage;
            _invHandler = invHandler;

            TrySnapshot();
            Log.Debug("[InvPage] Open OK, snapshot inicial feito.");
            return _page != null;
        }

        public override void Close()
        {
            _before.Clear();
            _page = null;
            _root = null;
            _invHandler = null;
            Log.Debug("[InvPage] Close.");
        }

        public void Update()
        {
            if (!DetectInventoryChanged())
            {
                return;
            }

            Log.Debug("[InvPage] Change detected → HandleMerges + HandleSplits.");
            HandleMerges();
            HandleSplits();
            TrySnapshot();
        }

        private void TrySnapshot()
        {
            _before.Clear();
            IList<Item> inv = Inv;
            if (inv == null)
            {
                return;
            }

            for (int i = 0; i < inv.Count; i++)
            {
                _before[i] = inv[i] is SObject o
                    ? new Snap
                    {
                        Qid = o.QualifiedItemId,
                        Stack = o.Stack,
                        Pool = DurabilityPool.ReadPool(o)
                    }
                    : new Snap { Qid = null, Stack = 0, Pool = 0f };
            }
        }

        private bool DetectInventoryChanged()
        {
            IList<Item> inv = Inv;
            if (inv == null || inv.Count != _before.Count)
            {
                return false;
            }

            for (int i = 0; i < inv.Count; i++)
            {
                Item now = inv[i];
                Snap had = _before[i];

                if (now == null)
                {
                    if (had.Qid != null || had.Stack != 0)
                    {
                        return true;
                    }

                    continue;
                }

                SObject nowObj = now as SObject;
                string nowQid = nowObj?.QualifiedItemId ?? now.QualifiedItemId;
                if (nowQid != had.Qid || now.Stack != had.Stack)
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleMerges()
        {
            IList<Item> inv = Inv;
            if (inv == null)
            {
                return;
            }

            for (int a = 0; a < inv.Count; a++)
            {
                if (inv[a] is not SObject dst || !DurabilityPool.IsTackle(dst))
                {
                    continue;
                }

                if (!_before.TryGetValue(a, out Snap preA))
                {
                    continue;
                }

                if (dst.Stack <= preA.Stack)
                {
                    continue;
                }

                string q = dst.QualifiedItemId;
                int need = dst.Stack - preA.Stack;
                float addPool = 0f;

                for (int b = 0; b < inv.Count && need > 0; b++)
                {
                    if (b == a)
                    {
                        continue;
                    }

                    SObject srcNow = inv[b] as SObject;
                    _ = _before.TryGetValue(b, out Snap preB);

                    bool wasSame = preB.Qid == q && preB.Stack > 0;
                    bool decreased =
                        (srcNow == null && preB.Stack > 0) ||
                        (srcNow != null && srcNow.Stack < preB.Stack);

                    if (!wasSame || !decreased)
                    {
                        continue;
                    }

                    int moved = preB.Stack - (srcNow?.Stack ?? 0);
                    if (moved <= 0)
                    {
                        continue;
                    }

                    float frac = moved / (float)preB.Stack;
                    addPool += preB.Pool * frac;
                    need -= moved;
                }

                if (addPool > 0f)
                {
                    float dstPool = DurabilityPool.ReadPool(dst);
                    DurabilityPool.WritePool(dst, dstPool + addPool);
                    DurabilityPool.NormalizeStack(dst);
                    Log.Debug($"[InvPage] merge: +pool={addPool:0.###} → pool={DurabilityPool.ReadPool(dst):0.###}, stack={dst.Stack}");
                }
            }
        }

        private void HandleSplits()
        {
            IList<Item> inv = Inv;
            if (inv == null)
            {
                return;
            }

            for (int i = 0; i < inv.Count; i++)
            {
                if (inv[i] is not SObject now || !DurabilityPool.IsTackle(now))
                {
                    continue;
                }

                _ = _before.TryGetValue(i, out Snap pre);
                if (pre.Qid == now.QualifiedItemId && pre.Stack == 0 && now.Stack == 1)
                {
                    int srcIdx = -1;
                    for (int j = 0; j < inv.Count; j++)
                    {
                        if (j == i)
                        {
                            continue;
                        }

                        SObject srcNow = inv[j] as SObject;
                        _ = _before.TryGetValue(j, out Snap preJ);

                        bool wasSame = preJ.Qid == now.QualifiedItemId && preJ.Stack > 0;
                        bool decreased =
                            (srcNow == null && preJ.Stack > 0) ||
                            (srcNow != null && srcNow.Stack < preJ.Stack);

                        if (wasSame && decreased)
                        {
                            srcIdx = j;
                            break;
                        }
                    }

                    if (srcIdx >= 0)
                    {
                        if (inv[srcIdx] is SObject src)
                        {
                            _ = DurabilityPool.MaterializeOneFromStack(src, now);
                            DurabilityPool.NormalizeStack(src);
                            if (now is SObject single)
                            {
                                DurabilityPool.WritePool(single, 0f);
                            }

                            Log.Debug($"[InvPage] split: out uses set; src normalized → pool={DurabilityPool.ReadPool(src):0.###}, stack={src.Stack}");
                        }
                        else
                        {
                            Log.Warn($"[InvPage] split: source slot {srcIdx} ficou null; não deu pra materializar.");
                        }
                    }
                }
            }
        }

        private static class DurabilityPool
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

                int max = GetMaxUses(rep);
                int used = (frac > 0f)
                    ? (int)Math.Round((1f - frac) * max, MidpointRounding.AwayFromZero)
                    : 0;

                SetUsesConsumed(rep, used);
                WritePool(rep, Math.Clamp(p, 0f, rep.Stack));
            }


            public static float ReadPool(SObject rep)
            {
                if (rep.modData.TryGetValue(KeyPool, out string s) &&
                    float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                {
                    return Math.Clamp(f, 0f, rep.Stack);
                }

                float baseOne = GetRemainingFraction(rep);
                float pool = baseOne + Math.Max(0, rep.Stack - 1);
                return Math.Clamp(pool, 0f, rep.Stack);
            }

            public static void WritePool(SObject rep, float pool)
            {
                pool = Math.Clamp(pool, 0f, rep.Stack);
                rep.modData[KeyPool] = pool.ToString(CultureInfo.InvariantCulture);
            }

            public static float MaterializeOneFromStack(SObject stackRep, SObject singleOut)
            {
                float pool = ReadPool(stackRep);
                int max = GetMaxUses(stackRep);

                int used;
                float taken;

                if (pool >= 1f)
                {
                    used = 0;
                    taken = 1f;
                    pool -= 1f;
                }
                else
                {
                    taken = pool;
                    used = (int)Math.Round((1f - taken) * max, MidpointRounding.AwayFromZero);
                    pool = 0f;
                }

                SetUsesConsumed(singleOut, used);
                WritePool(stackRep, pool);
                NormalizeStack(stackRep);

                return taken;
            }


            public static int GetUsesLeft(SObject o)
            {
                try { return o.uses.Value; } catch { return GetMaxUses(o); }
            }

            public static void SetUsesLeft(SObject o, int uses)
            {
                try { o.uses.Value = uses; } catch { /* ignore */ }
            }

            public static int GetMaxUses(SObject o)
            {
                if (o.modData.TryGetValue(KeyMax, out string s) && int.TryParse(s, out int cached))
                {
                    return cached;
                }

                int max = 20;
                o.modData[KeyMax] = max.ToString(CultureInfo.InvariantCulture);
                return max;
            }

            public static float GetRemainingFraction(SObject o)
            {
                int max = GetMaxUses(o);
                int used = GetUsesConsumed(o);
                return max <= 0 ? 1f : Math.Clamp(1f - (used / (float)max), 0f, 1f);
            }

            public static int GetUsesConsumed(SObject o)
            {
                try { return o.uses.Value; } catch { return 0; }
            }

            public static void SetUsesConsumed(SObject o, int used)
            {
                try { o.uses.Value = Math.Max(0, used); } catch { /* ignore */ }
            }


        }
    }
}
