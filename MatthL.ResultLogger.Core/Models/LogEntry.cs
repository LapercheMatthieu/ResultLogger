using MatthL.ResultLogger.Core.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatthL.ResultLogger.Core.Models
{
    /// <summary>
    /// an Entry for the logs
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public string CallerMethod { get; set; }
        public string CallerFile { get; set; }
        public int CallerLine { get; set; }
        public string Error { get; set; }

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] {CallerMethod} - {Message} {(Error != null ? $"| Error: {Error}" : "")} | {Path.GetFileName(CallerFile)}:{CallerLine}";
        }
    }
}
