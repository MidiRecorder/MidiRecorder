using System.Reflection;
using CannedBytes.Midi;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MidiRecorder.CommandLine;
using MidiRecorder.CommandLine.Logging;

const string environmentVarPrefix = "MidiRecorder_";
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables(prefix: environmentVarPrefix)
    .Build();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.ClearProviders();
    builder.AddConfiguration(config.GetSection("Logging"));

    builder.AddConsole();
    builder.AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleFormatterOptions>();
});
ILogger logger = loggerFactory.CreateLogger("MidiRecorder");

using var parser = new Parser(with =>
{
    with.HelpWriter = null;
});

var parserResult = parser.ParseArguments<RecordOptions, ListMidiInputsOptions>(args);

return parserResult
    .MapResult<RecordOptions, ListMidiInputsOptions, int>(
        Record,
        ListMidiInputs,
        errors => DisplayHelp(parserResult, errors));

int ListMidiInputs(ListMidiInputsOptions options)
{
    var midiInCapabilities = new MidiInPortCapsCollection();

    if (midiInCapabilities.Count == 0)
    {
        LoggerMessage.Define<string>(LogLevel.Error, 0, "No MIDI inputs {Xxx}");
        logger.LogError("No MIDI inputs");
    }

    foreach (var x in midiInCapabilities.Select((port, idx) => (port, idx)))
    {
        Console.WriteLine($"{x.idx}. {x.port.Name}");
    }

    return 0;
}

int Record(RecordOptions options)
{
    var svc = new MidiRecorderApplicationService(logger);

    using RecordResult stop = svc.StartRecording(options);

    if (stop.IsError)
    {
        DisplayHelp(parserResult, Enumerable.Empty<Error>());
        return 1;
    }

    logger.LogInformation("Recording started, Press any key to quit");
    Console.ReadLine();
    return 0;
}

static int DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errors)
{
    var errs = errors.ToArray();

    if (errs.IsVersion())
    {
        var helpText = HelpText.AutoBuild(result);
        Console.WriteLine(helpText);
        return 0;
    }
    if (errs.IsHelp())
    {
        Console.WriteLine(GetHelpText(verbs: true));
        return 0;
    }

    Console.WriteLine(GetHelpText(verbs: false));
    return 1;

    string GetHelpText(bool verbs)
    {
        return HelpText.AutoBuild(result, h =>
            {
                h.AdditionalNewLineAfterOption = false;
                string? assemblyDescription = Assembly.GetExecutingAssembly()
                    .GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)
                    .OfType<AssemblyDescriptionAttribute>()
                    .FirstOrDefault()
                    ?.Description;
                if (errs.IsHelp())
                    h.AddPreOptionsLine(assemblyDescription);
                return HelpText.DefaultParsingErrorsHandler(result, h);
            },
            e => e,
            verbsIndex: verbs);
    }
}
