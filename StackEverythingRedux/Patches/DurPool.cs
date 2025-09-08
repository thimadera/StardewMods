using System;
using System.Globalization;
using SObject = StardewValley.Object;

namespace StackEverythingRedux.Patches
{
    internal static class DurPool
    {
        private const string KeyPool = "ser.durPool";
        private const string KeyMax = "ser.maxUses";

        public static float Read(SObject o)
        {
            if (o.modData.TryGetValue(KeyPool, out string s) &&
                float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
            {
                return Math.Clamp(f, 0f, Math.Max(1, o.Stack));
            }

            // default: frac do primeiro + os demais cheios
            float one = RemFraction(o);
            return one + Math.Max(0, o.Stack - 1);
        }

        public static void Write(SObject o, float pool)
        {
            pool = Math.Clamp(pool, 0f, Math.Max(1, o.Stack));
            o.modData[KeyPool] = pool.ToString(CultureInfo.InvariantCulture);
        }

        public static void Normalize(SObject o)
        {
            float p = Read(o);
            int whole = (int)Math.Floor(p);
            float frac = p - whole;

            int newStack = whole + (frac > 0 ? 1 : 0);
            if (newStack <= 0)
            {
                newStack = 1;
            }

            o.Stack = newStack;

            int max = MaxUses(o);
            int uses = frac > 0
                ? (int)Math.Round(frac * max, MidpointRounding.AwayFromZero)
                : max;

            SetUses(o, uses);
            Write(o, Math.Clamp(p, 0f, o.Stack));
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
            // default vanilla tackle max
            int def = 20;
            o.modData[KeyMax] = def.ToString(CultureInfo.InvariantCulture);
            return def;
        }

        private static int GetUses(SObject o) { try { return o.uses.Value; } catch { return MaxUses(o); } }
        private static void SetUses(SObject o, int uses) { try { o.uses.Value = uses; } catch { } }
    }
}
