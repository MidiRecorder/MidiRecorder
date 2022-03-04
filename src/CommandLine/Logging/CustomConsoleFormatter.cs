using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace MidiRecorder.CommandLine.Logging;

public sealed class CustomConsoleFormatter : ConsoleFormatter, IDisposable
{
    private CustomConsoleFormatterOptions _formatterOptions;
    private readonly IDisposable _optionsReloadToken;

    public CustomConsoleFormatter(IOptionsMonitor<CustomConsoleFormatterOptions> options) : base(nameof(CustomConsoleFormatter))
    {
        _optionsReloadToken = options.OnChange(o => _formatterOptions = o);
        _formatterOptions = options.CurrentValue;
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider scopeProvider,
        TextWriter textWriter)
    {
        var logLevel = logEntry.LogLevel;
        var exception = logEntry.Exception;
        var state = logEntry.State;
        var formatter = logEntry.Formatter;

        var formattedMessage = formatter?.Invoke(state, exception) ?? state?.ToString();

        ConsoleColor? foregroundColor =
            _formatterOptions.EnableColors
                ? logLevel switch
                {
                    LogLevel.Trace => ConsoleColor.DarkGray,
                    LogLevel.Debug => ConsoleColor.Blue,
                    LogLevel.Information => ConsoleColor.Green,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Error => ConsoleColor.Red,
                    LogLevel.Critical => ConsoleColor.Magenta,
                    LogLevel.None => null,
                    _ => null
                }
                : null;


        var logRecord = exception == null
            ? $"{formattedMessage}"
            : $"{formattedMessage} {exception.ToStringDemystified()}";
        textWriter.SetForegroundColor(foregroundColor);
        textWriter.WriteLine(logRecord);
        textWriter.ResetForegroundColor(foregroundColor);
    }

    public void Dispose() => _optionsReloadToken.Dispose();
}
