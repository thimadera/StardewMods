using StardewModdingAPI;

namespace StackEverythingRedux
{
    public static class Log
    {
        private static bool Ready => StackEverythingRedux.Instance?.Monitor != null;

        public static void Alert(string msg)
        {
            if (Ready)
            {
                StackEverythingRedux.Instance.Monitor.Log(msg, LogLevel.Alert);
            }
        }
        public static void Error(string msg)
        {
            if (Ready)
            {
                StackEverythingRedux.Instance.Monitor.Log(msg, LogLevel.Error);
            }
        }
        public static void Warn(string msg)
        {
            if (Ready)
            {
                StackEverythingRedux.Instance.Monitor.Log(msg, LogLevel.Warn);
            }
        }
        public static void Info(string msg)
        {
            if (Ready)
            {
                StackEverythingRedux.Instance.Monitor.Log(msg, LogLevel.Info);
            }
        }
        public static void Debug(string msg)
        {
            if (Ready)
            {
                StackEverythingRedux.Instance.Monitor.Log(msg, LogLevel.Debug);
            }
        }
        public static void Trace(string msg)
        {
            if (Ready)
            {
                StackEverythingRedux.Instance.Monitor.Log(msg, LogLevel.Trace);
            }
        }

        public static void TraceIfD(string msg)
        {
#if DEBUG
            bool debugging = true;
#else
            bool debugging = StackEverythingRedux.Config?.DebuggingMode == true;
#endif
            if (debugging)
            {
                Trace(msg);
            }
        }
    }
}
