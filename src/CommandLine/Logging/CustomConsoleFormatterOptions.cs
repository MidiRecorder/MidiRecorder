using Microsoft.Extensions.Logging.Console;

namespace MidiRecorder.CommandLine.Logging;

public class CustomConsoleFormatterOptions : ConsoleFormatterOptions
{
    public bool EnableColors { get; set; }
}
