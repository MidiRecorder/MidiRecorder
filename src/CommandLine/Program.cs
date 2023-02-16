using System.Reflection;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MidiRecorder.Application;
using MidiRecorder.Application.Implementation;
using MidiRecorder.CommandLine;
using MidiRecorder.CommandLine.Logging;

const string environmentVarPrefix = "MidiRecorder_";
IConfigurationRoot config = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, false)
    .AddEnvironmentVariables(environmentVarPrefix).Build();

using ILoggerFactory? loggerFactory = LoggerFactory.Create(
    builder =>
    {
        builder.ClearProviders();
        builder.AddConfiguration(config.GetSection("Logging"));

        builder.AddConsole();
        builder.AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleFormatterOptions>();
    });
ILogger logger = loggerFactory.CreateLogger("MidiRecorder");

using var parser = new Parser(with => { with.HelpWriter = null; });

var parserResult = parser.ParseArguments<RecordOptions, ListMidiInputsOptions>(args);

return parserResult.MapResult<RecordOptions, ListMidiInputsOptions, int>(
    Record,
    ListMidiInputs,
    errors => DisplayHelp(parserResult, errors));

int ListMidiInputs(ListMidiInputsOptions options)
{
    var midiInputService = new MidiInputService(loggerFactory.CreateLogger<MidiInputService>());
    var midiInCapabilities = midiInputService.GetMidiInputs().ToArray();

    if (!midiInCapabilities.Any())
    {
        logger.LogError("No MIDI inputs");
    }

    foreach ((MidiInput midiInput, int idx) x in midiInCapabilities.Select((midiInput, idx) => (midiInput, idx)))
    {
        Console.WriteLine($"{x.idx}. {x.midiInput.Name}");
    }

    return 0;
}

int Record(RecordOptions options)
{
    var midiInputService = new MidiInputService(loggerFactory.CreateLogger<MidiInputService>());
    var validator = new OptionsValidator(midiInputService);
    (TypedRecordOptions? typedOptions, var errorMessage) = validator.Validate(options);

    if (typedOptions == null)
    {
        Console.WriteLine(errorMessage);
        DisplayHelp(parserResult, Enumerable.Empty<Error>());
        return 1;
    }

    var sourceBuilder = new NAudioMidiSourceBuilder();
    var saver = new NAudioMidiFileSaver();
    var analyzer = new NAudioMidiEventAnalyzer();
    var splitter = new MidiSplitter<MidiEventWithPort>();
    var trackBuilder = new NAudioMidiTrackBuilder();
    var svc = new MidiRecorderApplicationService<MidiEventWithPort>(
        sourceBuilder,
        loggerFactory.CreateLogger<MidiRecorderApplicationService<MidiEventWithPort>>(),
        saver,
        analyzer,
        splitter,
        trackBuilder);

    using IDisposable _ = svc.StartRecording(typedOptions);
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
        Console.WriteLine(GetHelpText(true));
        return 0;
    }

    Console.WriteLine(GetHelpText(false));
    return 1;

    string GetHelpText(bool verbs)
    {
        return HelpText.AutoBuild(
            result,
            h =>
            {
                h.AdditionalNewLineAfterOption = false;
                var assemblyDescription = Assembly.GetExecutingAssembly()
                    .GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)
                    .OfType<AssemblyDescriptionAttribute>().FirstOrDefault()?.Description;
                if (errs.IsHelp())
                {
                    h.AddPreOptionsLine(assemblyDescription);
                }

                return HelpText.DefaultParsingErrorsHandler(result, h);
            },
            e => e,
            verbs);
    }
}
