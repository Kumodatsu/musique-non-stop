using System;
using System.IO;
using System.Threading.Tasks;
using Discord;

namespace Kumodatsu.MusiqueNonStop;

static class Logger {

    private static readonly TextWriter writer = Console.Out;

    public static LogSeverity SeverityLevel { get; set; }
        = LogSeverity.Debug;

    public static async Task LogAsync(
        string      message,
        string      source    = "Bot",
        LogSeverity severity  = LogSeverity.Info,
        Exception?  exception = null
    ) {
        if (severity > SeverityLevel)
            return;
        var time = DateTime.Now.ToString("HH:mm:ss");
        await writer.WriteLineAsync(
            $"[{time}] [{source}] [{severity}]: {message}"
            + (exception is not null
                ? $"\nException: {exception.Message}"
                : string.Empty
            )
            + (exception?.StackTrace is not null
                ? $"\n{exception.StackTrace}"
                : string.Empty
            )
        );
    }

}
