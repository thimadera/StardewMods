using System;
using System.Globalization;
using StardewValley;
using SObject = StardewValley.Object;

namespace StackEverythingRedux.Patches
{
    internal static class TackleAddToStackPatches
    {
        private static SObject _preDest, _preSrc;
        private static int _preDestStack, _preSrcStack;
        private static float _preDestPool, _preSrcPool;

        public static void Prefix(Item __instance, Item __0)
        {
            _preDest = __instance as SObject;
            _preSrc = __0 as SObject;

            if (!IsTackleMerge(_preDest, _preSrc))
            {
                return;
            }

            _preDestStack = _preDest.Stack;
            _preSrcStack = _preSrc.Stack;
            _preDestPool = Pool.Read(_preDest);
            _preSrcPool = Pool.Read(_preSrc);

            Log.Debug($"[AddToStack/Pre] dest={Desc(_preDest)} pool={_preDestPool:0.###} | src={Desc(_preSrc)} pool={_preSrcPool:0.###}");
        }

        public static void Postfix(Item __instance, Item __0)
        {
            SObject dest = __instance as SObject;
            SObject src = __0 as SObject;

            if (!IsTackleMerge(dest, src))
            {
                return;
            }

            int moved = Math.Max(0, dest.Stack - _preDestStack);
            if (moved == 0)
            {
                Log.Trace("[AddToStack/Post] moved=0 → nada a fazer.");
                return;
            }

            float movedFrac = (_preSrcStack > 0) ? (_preSrcPool * (moved / (float)_preSrcStack)) : 0f;

            float dstPool = Pool.Read(dest);
            Pool.Write(dest, dstPool + movedFrac);

            if (src.Stack >= 0)
            {
                float newSrcPool = Math.Max(0f, _preSrcPool - movedFrac);
                Pool.Write(src, newSrcPool);
            }

            Pool.Normalize(dest);
            if (src.Stack > 0)
            {
                Pool.Normalize(src);
            }

            Log.Debug($"[AddToStack/Post] moved={moved} | dest→ {Desc(dest)} pool={Pool.Read(dest):0.###} | src→ {Desc(src)} pool={Pool.Read(src):0.###}");
        }

        private static bool IsTackleMerge(SObject a, SObject b)
        {
            return a != null && b != null &&
            a.Category == -22 && b.Category == -22 &&
            a.QualifiedItemId == b.QualifiedItemId;
        }

        private static string Desc(SObject o)
        {
            return o == null ? "null" : $"{o.Name} qid={o.QualifiedItemId} stack={o.Stack} uses={TryUses(o)}";
        }

        private static int TryUses(SObject o) { try { return o.uses.Value; } catch { return -1; } }

        private static class Pool
        {
            private const string KeyPool = "ser.durPool";
            private const string KeyMax = "ser.maxUses";

            public static float Read(SObject rep)
            {
                if (rep.modData.TryGetValue(KeyPool, out string s) &&
                    float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                {
                    return Math.Clamp(f, 0f, rep.Stack);
                }

                float one = RemFraction(rep);
                float pool = one + Math.Max(0, rep.Stack - 1);
                return Math.Clamp(pool, 0f, rep.Stack);
            }

            public static void Write(SObject rep, float pool)
            {
                pool = Math.Clamp(pool, 0f, Math.Max(1, rep.Stack));
                rep.modData[KeyPool] = pool.ToString(CultureInfo.InvariantCulture);
            }

            public static void Normalize(SObject rep)
            {
                float p = Read(rep);
                int whole = (int)Math.Floor(p);
                float frac = p - whole;

                int newStack = whole + (frac > 0f ? 1 : 0);
                if (newStack == 0)
                {
                    newStack = 1;
                }

                rep.Stack = newStack;

                int max = MaxUses(rep);
                int uses = (frac > 0f)
                    ? (int)Math.Round(frac * max, MidpointRounding.AwayFromZero)
                    : max;

                SetUses(rep, uses);
                Write(rep, Math.Clamp(p, 0f, rep.Stack));
            }

            public static float RemFraction(SObject o)
            {
                int max = MaxUses(o);
                int left = GetUses(o);
                return max <= 0 ? 1f : Math.Clamp(left / (float)max, 0f, 1f);
            }

            public static int MaxUses(SObject o)
            {
                if (o.modData.TryGetValue(KeyMax, out string s) && int.TryParse(s, out int v))
                {
                    return v;
                }

                int def = 20;
                o.modData[KeyMax] = def.ToString(CultureInfo.InvariantCulture);
                return def;
            }

            private static int GetUses(SObject o) { try { return o.uses.Value; } catch { return MaxUses(o); } }
            private static void SetUses(SObject o, int uses) { try { o.uses.Value = uses; } catch { } }
        }
    }
}
