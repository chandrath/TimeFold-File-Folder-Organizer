using System;
using System.Diagnostics;
using System.IO;

namespace FileOrganizer.Services
{
    public static class PerfLogger
    {
        private static readonly object _lock = new();
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "timefold-perf.log");

        public static bool EnableDiskLogging = false;

        [Conditional("DEBUG")]
        public static void Log(string message)
        {
            if (!EnableDiskLogging) return;
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(LogFilePath, line + Environment.NewLine);
                }
            }
            catch { }
        }

        public static IDisposable? Measure(string operationName)
        {
#if DEBUG
            if (EnableDiskLogging) return new OperationScope(operationName);
#endif
            return null;
        }

        private class OperationScope : IDisposable
        {
            private readonly string _name;
            private readonly Stopwatch _sw;

            public OperationScope(string name)
            {
                _name = name;
                Log($"⏱ START: {_name}");
                _sw = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _sw.Stop();
                Log($"⏱ END:   {_name} -> {_sw.ElapsedMilliseconds} ms ({_sw.ElapsedTicks} ticks)");
            }
        }
    }
}
