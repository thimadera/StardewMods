using StardewModdingAPI;

namespace Thimadera.StardewMods.StackEverythingRedux
{
    /// <summary>
    /// Convenience class so we don't have to keep passing Mod.Instance.Monitor everywhere
    /// </summary>
    public static class Log
    {
        public static void Alert(string msg)
        {
            StackEverythingRedux.Instance.Monitor.Log(msg, LogLevel.Alert);
        }

        public static void Error(string msg)
        {
            StackEverythingRedux.Instance.Monitor.Log(msg, LogLevel.Error);
        }

        public static void Warn(string msg)
        {
            StackEverythingRedux.Instance.Monitor.Log(msg, LogLevel.Warn);
        }

        public static void Info(string msg)
        {
            StackEverythingRedux.Instance.Monitor.Log(msg, LogLevel.Info);
        }

        public static void Debug(string msg)
        {
            StackEverythingRedux.Instance.Monitor.Log(msg, LogLevel.Debug);
        }

        public static void Trace(string msg)
        {
            StackEverythingRedux.Instance.Monitor.Log(msg, LogLevel.Trace);
        }

        public static void TraceIfD(string msg)
        {
#if DEBUG
            bool debugging = true;
#else
            bool debugging = Mod.Config.DebuggingMode;
#endif
            if (debugging)
            {
                Trace(msg);
            }
        }
    }
    public static class Seq
    {
        public static int Min(params int[] args)
        {
            return args.Min();
        }
    }
}
