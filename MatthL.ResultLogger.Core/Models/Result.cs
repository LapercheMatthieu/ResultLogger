using MatthL.ResultLogger.Core.Enums;
using MatthL.ResultLogger.Core.Managers;
using System.Runtime.CompilerServices;

namespace MatthL.ResultLogger.Core.Models
{
    /// <summary>
    /// Simple Result class to transfert message and show in log without value shared
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string Error { get; }

        protected Result(bool isSuccess, string error,
            string message = "",
            LogLevel level = LogLevel.Undefined,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            IsSuccess = isSuccess;
            Error = error;

            // Logger automatiquement
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = isSuccess ? LogLevel.Debug : LogLevel.Error,
                Message = isSuccess ? "Operation succeeded" : "Operation failed",
                CallerMethod = caller,
                CallerFile = file,
                CallerLine = line,
                Error = error
            };
            if(message != "")
            {
                logEntry.Message = message;
            }
            if (level != LogLevel.Undefined)
            {
                logEntry.Level = level;
            }

            LogManager.Log(logEntry);
        }



        public static Result Success(string message = "", LogLevel level = LogLevel.Undefined, [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
            => new Result(true, null, message,level, caller, file, line);

        public static Result Failure(string error, string message = "", LogLevel level = LogLevel.Undefined,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
            => new Result(false, error, message, level, caller, file, line);

        public static Result Combine(params Result[] results)
        {
            foreach (var result in results)
            {
                if (result.IsFailure)
                    return result;
            }
            return Success();
        }
    }

    /// <summary>
    /// Variante of the Result Class that can transport a typed class T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Result<T> : Result
    {
        public T Value { get; }

        protected Result(T value, bool isSuccess, string error, string message, LogLevel level,
            string caller, string file, int line)
            : base(isSuccess, error, message, level, caller, file, line)
        {
            Value = value;
        }

        public static Result<T> Success(T value, string message = "", LogLevel level = LogLevel.Undefined,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
            => new Result<T>(value, true, null, message, level, caller, file, line);


        public static new Result<T> Failure(string error, string message = "", LogLevel level = LogLevel.Undefined,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
            => new Result<T>(default, false, error, message, level, caller, file, line);

        public Result<U> Map<U>(Func<T, U> mapper)
        {
            return IsSuccess
                ? Result<U>.Success(mapper(Value))
                : Result<U>.Failure(Error);
        }
    }

}
