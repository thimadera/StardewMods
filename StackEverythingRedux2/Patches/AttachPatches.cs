using System.Text;
using StardewValley;
using Tool = StardewValley.Tool;

namespace StackEverythingRedux.Patches
{
    internal class AttachPatches
    {
        private static Object _incoming;
        private static string _incomingQid;
        private static int _capturedStack;
        private static int _preSlotIndex = -1;
        private static int _preSlotStack = 0;
        private static bool _preOtherSlotEmpty;

        private static string DescribeObj(Object o)
        {
            return o == null ? "null" : $"{o.Name ?? "?"} id={o.QualifiedItemId} cat={o.Category} stack={o.Stack}";
        }

        private static string DescribeSlots(Tool t)
        {
            if (t?.attachments == null)
            {
                return "attachments=null";
            }

            StringBuilder sb = new StringBuilder().Append($"slots[{t.attachments.Count}]: ");
            for (int i = 0; i < t.attachments.Count; i++)
            {
                Object a = t.attachments[i];
                _ = sb.Append($"[{i}:{(a == null ? "null" : $"{a.Name} x{a.Stack}")}]");
                if (i < t.attachments.Count - 1)
                {
                    _ = sb.Append(' ');
                }
            }
            return sb.ToString();
        }

        public static void Prefix(Tool __instance, Object o)
        {
            _incoming = null;
            _incomingQid = null;
            _capturedStack = 0;
            _preSlotIndex = -1;
            _preSlotStack = 0;
            _preOtherSlotEmpty = false;

            Log.Debug($"[Attach/Prefix] tool={__instance?.GetType().Name}:{__instance?.QualifiedItemId} | arg={DescribeObj(o)} | {DescribeSlots(__instance)}");

            if (__instance?.QualifiedItemId != "(T)AdvancedIridiumRod" || o == null || o.Category != -22)
            {
                Log.Trace("[Attach/Prefix] not AIR or not tackle → skip.");
                return;
            }

            _incoming = o;
            _incomingQid = o.QualifiedItemId;
            _capturedStack = o.Stack;

            if (__instance.attachments != null && __instance.attachments.Count >= 3)
            {
                for (int i = 1; i <= 2; i++)
                {
                    Object a = __instance.attachments[i];
                    if (a != null && a.QualifiedItemId == _incomingQid)
                    {
                        _preSlotIndex = i;
                        _preSlotStack = a.Stack;
                        _preOtherSlotEmpty = __instance.attachments[3 - i] == null;
                        break;
                    }
                }
            }

            Log.Debug($"[Attach/Prefix] captured={DescribeObj(_incoming)} stackBefore={_capturedStack} | preSlotIndex={_preSlotIndex} preStack={_preSlotStack} otherEmpty={_preOtherSlotEmpty}");
        }

        public static void Postfix(Tool __instance)
        {
            Log.Debug($"[Attach/Postfix] after vanilla | {DescribeSlots(__instance)} | captured={DescribeObj(_incoming)} preSlotIndex={_preSlotIndex} preStack={_preSlotStack} otherEmpty={_preOtherSlotEmpty}");

            try
            {
                if (_incoming == null || _incomingQid == null || __instance?.attachments == null || __instance.attachments.Count < 3)
                {
                    Log.Trace("[Attach/Postfix] bail: missing pre-state or attachments.");
                    return;
                }
                if (_preSlotIndex is not (1 or 2) || !_preOtherSlotEmpty)
                {
                    Log.Trace("[Attach/Postfix] bail: no matching pre-slot or other slot wasn't empty.");
                    return;
                }

                int filledIdx = _preSlotIndex;
                int emptyIdx = 3 - filledIdx;
                Object filled = __instance.attachments[filledIdx];
                Object empty = __instance.attachments[emptyIdx];

                if (filled == null || filled.QualifiedItemId != _incomingQid)
                {
                    Log.Trace("[Attach/Postfix] bail: filled slot item changed or mismatched.");
                    return;
                }
                if (empty != null)
                {
                    Log.Trace("[Attach/Postfix] bail: empty slot is no longer empty.");
                    return;
                }
                if (filled.Stack <= _preSlotStack)
                {
                    Log.Trace($"[Attach/Postfix] bail: filled stack did not increase (pre={_preSlotStack}, now={filled.Stack}).");
                    return;
                }

                const int transfer = 1;
                if (filled.Stack < transfer)
                {
                    Log.Warn($"[Attach/Postfix] unexpected: filled stack < transfer ({filled.Stack} < {transfer})");
                    return;
                }

                _incoming.Stack = transfer;
                __instance.attachments[emptyIdx] = _incoming;

                int before = filled.Stack;
                filled.Stack = before - transfer;
                if (filled.Stack <= 0)
                {
                    __instance.attachments[filledIdx] = null;
                }

                Log.Debug($"[Attach/Postfix] moved {transfer} to slot {emptyIdx} and decremented slot {filledIdx}: {before}→{filled.Stack} | {DescribeSlots(__instance)}");
            }
            finally
            {
                _incoming = null;
                _incomingQid = null;
                _capturedStack = 0;
                _preSlotIndex = -1;
                _preSlotStack = 0;
                _preOtherSlotEmpty = false;
            }
        }
    }
}
