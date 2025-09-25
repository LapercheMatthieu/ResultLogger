using MatthL.ResultLogger.Core.Enums;
using MatthL.ResultLogger.Core.Models;
using System.Collections.Concurrent;
using System.IO;

namespace MatthL.ResultLogger.Core.Managers
{
    public static class LogManager
    {
        private static readonly ConcurrentQueue<LogEntry> _memoryLogs = new();
        private static LogDestination _destination = LogDestination.Memory;
        private static string _logFilePath;
        private static readonly object _fileLock = new();
        private static int _maxMemoryLogs = 10000;

        /// <summary>
        /// To configure the type of logging
        /// </summary>
        /// <param name="destination">memory, temps or custom file</param>
        /// <param name="customPath"> needed of custom file selected </param>
        /// <param name="maxMemoryLogs"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void Configure(LogDestination destination = LogDestination.Memory, string customPath = null, int maxMemoryLogs = 10000)
        {
            _destination = destination;
            _maxMemoryLogs = maxMemoryLogs;

            switch (destination)
            {
                case LogDestination.TempFile:
                    var tempPath = Path.GetTempPath();
                    var fileName = $"MatthL.ResultLogger.{DateTime.Now:yyyyMMdd_HHmmss}.log";
                    _logFilePath = Path.Combine(tempPath, fileName);
                    break;

                case LogDestination.CustomFile:
                    if (string.IsNullOrEmpty(customPath))
                        throw new ArgumentException("Custom path required for CustomFile destination");
                    _logFilePath = customPath;
                    var dir = Path.GetDirectoryName(_logFilePath);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    break;
            }
        }

        internal static void Log(LogEntry entry)
        {
            switch (_destination)
            {
                case LogDestination.Memory:
                    _memoryLogs.Enqueue(entry);
                    // Limiter la taille de la queue
                    while (_memoryLogs.Count > _maxMemoryLogs)
                    {
                        _memoryLogs.TryDequeue(out _);
                    }
                    break;

                case LogDestination.TempFile:
                case LogDestination.CustomFile:
                    WriteToFile(entry);
                    break;
            }
        }

        private static void WriteToFile(LogEntry entry)
        {
            lock (_fileLock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, entry.ToString() + Environment.NewLine);
                }
                catch
                {
                    // Fallback to memory if file write fails
                    _memoryLogs.Enqueue(entry);
                }
            }
        }

        public static List<LogEntry> GetLogs(int count = -1)
        {
            var logs = _memoryLogs.ToList();
            logs.Reverse();
            return count > 0 ? logs.Take(count).ToList() : logs;
        }

        public static void Clear()
        {
            while (_memoryLogs.TryDequeue(out _)) { }
        }

        public static string GetLogFilePath() => _logFilePath;
    }
}

