using System.Diagnostics;
using System.Reactive.Linq;
using System.Reflection;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MidiRecorder.Application;
using MidiRecorder.Application.Implementation;
using MidiRecorder.CommandLine;
using MidiRecorder.CommandLine.Logging;
using NAudio.Midi;

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
    return parserResult.MapResult<RecordOptions, ListMidiInputsOptions, int>(
        Record,
        ListMidiInputs,
        errors => DisplayHelp(parserResult, errors));
}
#pragma warning disable CA1031 Topmost catch to present exception
catch (Exception ex)
#pragma warning restore CA1031
{
    logger.LogCritical(ex.Demystify(), "{Message}", ex.Message);
    return 1;
}

int ListMidiInputs(ListMidiInputsOptions options) =>
    NAudioMidiInputs.GetMidiInputs().ToSeq()
        .Match(
            () =>
            {
                logger.LogError("{Message}", "No MIDI inputs");
                return 1;
            },
            midiInCapabilities =>
            {
                _ = midiInCapabilities.Iter((i, midiInput) => Console.WriteLine($"{i}. {midiInput.Name}"));
                return 0;
            });

int Record(RecordOptions options) =>
    options
        .Validate(
            NAudioMidiInputs.SearchMidiInputId,
            x => NAudioMidiFormatTester.TestFormat(x, NAudioMidiEventAnalyzer.IsNote))
        .Match(
            typedOptions =>
            {
                _ = typedOptions.MidiInputs.Iter(tuple => logger.LogInformation("Using MIDI input {MidiInputId} ({MidiInputName})", tuple.Item1, tuple.Item2));
                var source = new NAudioMidiSource(typedOptions);
                PrintOptions(typedOptions, logger);
                var allEvents = source.AllEvents;

                NoteDuration.CalculateDurations(
                    allEvents,
                    x => x.MidiEvent is NoteEvent { CommandCode: MidiCommandCode.NoteOn },
                    x => x.MidiEvent is NoteEvent { CommandCode: MidiCommandCode.NoteOff },
                    x => x.MidiEvent is NoteEvent n ? n.NoteNumber : 0)
                    .ForEachAsync(x =>
                    {
                        if (x.duration == TimeSpan.Zero)
                        {
                            logger.LogTrace("{MidiEvent}", x.ev.MidiEvent);
                        }
                        else
                        {
                            logger.LogTrace(@"{MidiEvent} {Duration:s\.ff}", x.ev.MidiEvent, x.duration);
                        }
                    });
                
                var split = MidiSplitter.Split(
                    allEvents,
                    NAudioMidiEventAnalyzer.NoteAndSustainPedalCount,
                    typedOptions.TimeoutToSave,
                    typedOptions.DelayToSave);

                _ = split.AdjustedReleaseMarkers.ForEachAsync(_ => logger.LogTrace("All Notes/Pedals Off!"));
                _ = split.SplitGroups
                    .SelectMany(x => x.ToArray()
                        .Select(midiEvents =>
                        {
                            var filePath = MidiFileContext.BuildFilePath(
                                typedOptions.PathFormatString,
                                midiEvents,
                                DateTime.Now,
                                Guid.NewGuid(),
                                NAudioMidiEventAnalyzer.IsNote);
                            var tracks = NAudioMidiTrackBuilder.BuildTracks(midiEvents);
                            return (tracks, filePath);
                        }))
                    .ForEachAsync(x =>
                    {
                        logger.LogInformation(
                            "Saving {EventCount} events (plus 1 EndOfTrack) to file {FilePath}...",
                            x.tracks.Sum(y => y.Count()) - 1,
                            x.filePath);
                        try
                        {
                            NAudioMidiFileSaver.Save(x.tracks, x.filePath, typedOptions.MidiResolution);
                        }
#pragma warning disable CA1031
                        catch (Exception ex)
#pragma warning restore CA1031
                        {
                            logger.LogError(ex, "There was an error when saving the file");
                        }
                    });

                
                source.StartReceiving();
                
                logger.LogInformation("Recording started, Press any key to quit");
                Console.ReadLine();
                return 0;
            },
            errorMessage =>
            {
                logger.LogCritical("{Message}", errorMessage);
                DisplayHelp(parserResult, Enumerable.Empty<Error>());
                return 1;
            });

static void PrintOptions(TypedRecordOptions options, ILogger logger)
{
    (TimeSpan timeToSaveAfterAllOff, TimeSpan timeToSaveAfterHeldEvents, var pathFormatString, var midiResolution, _) = options;
#pragma warning disable CA1848
    logger.LogInformation("Working dir: {CurrentDirectory}", Environment.CurrentDirectory);
    logger.LogInformation("Delay to save: {DelayToSave}", timeToSaveAfterAllOff);
    logger.LogInformation("Timeout to save: {TimeoutToSave}", timeToSaveAfterHeldEvents);
    logger.LogInformation("Output Path: {PathFormatString}", pathFormatString);
    logger.LogInformation("MIDI resolution: {MidiResolution}", midiResolution);
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
                    .OfType<AssemblyDescriptionAttribute>()
                    .FirstOrDefault()
                    ?.Description;
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
