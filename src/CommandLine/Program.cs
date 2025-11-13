using System.Diagnostics;
using System.Reflection;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MidiRecorder.Application.Implementation;
using MidiRecorder.Application.ListMidiInputs;
using MidiRecorder.Application.Record;
using MidiRecorder.CommandLine.ListMidiInputs;
using MidiRecorder.CommandLine.Logging;
using AssemblyExtensions = MidiRecorder.Application.Record.AssemblyExtensions;

const string environmentVarPrefix = "MidiRecorder_";
IConfigurationRoot config = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, false)
    .AddEnvironmentVariables(environmentVarPrefix)
    .Build();

using ILoggerFactory loggerFactory = LoggerFactory.Create(
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

try
{

    return parserResult.MapResult<IRecordOptions, ListMidiInputsOptions, int>(
        options =>
        {
            var appService = new RecordService<NAudioMidiEvent>(
                NAudioMidiFormatTester.TestFormat,
                loggerFactory.CreateLogger<RecordService<NAudioMidiEvent>>(),
                new NAudioMidiTools(),
                errorMessage =>
                {
                    logger.LogCritical("{Message}", errorMessage);
                    DisplayHelp(parserResult, Enumerable.Empty<Error>());
                    return 1;
                });
            return appService.Record(options);
        },
        _ =>
        {
            var appService = new ListMidiInputsService<NAudioMidiEvent>(
                loggerFactory.CreateLogger<ListMidiInputsService<NAudioMidiEvent>>(),
                new NAudioMidiTools());
            return appService.ListMidiInputs();
        },
        errors => DisplayHelp(parserResult, errors));
}
#pragma warning disable CA1031 Topmost catch to present exception
catch (Exception ex)
#pragma warning restore CA1031
{
    logger.LogCritical(ex.Demystify(), "{Message}", ex.Message);
    return 1;
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
                var assemblyDescription = AssemblyExtensions.Get<AssemblyDescriptionAttribute>()?.Description;
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
