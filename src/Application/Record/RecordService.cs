using System.Reactive.Linq;
using System.Reflection;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace MidiRecorder.Application.Record;

public class RecordService<TMidiEvent> where TMidiEvent : IMidiEvent
{
    private readonly Func<string, Validation<string, Unit>> _testFormat;
    private readonly ILogger<RecordService<TMidiEvent>> _logger;
    private readonly MidiTools<TMidiEvent> _midiTools;
    private readonly Func<Seq<string>, int> _handleError;

    public RecordService(
        Func<string, Validation<string, Unit>> testFormat,
        ILogger<RecordService<TMidiEvent>> logger,
        MidiTools<TMidiEvent> midiToolsService,
        Func<Seq<string>, int> handleError)
    {
        _testFormat = testFormat;
        _logger = logger;
        _midiTools = midiToolsService;
        _handleError = handleError;
    }

    public int Record(IRecordOptions options)
    {
        var product = AssemblyExtensions.Get<AssemblyProductAttribute>()?.Product;
        var version = AssemblyExtensions.Get<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
    
        _logger.LogInformation("{Product} {ProgramVersion}", product, version);

        return OptionsValidator.Validate(options, _midiTools.SearchMidiInputId, x => _testFormat(x))
            .Match(
                Record,
                _handleError);
    }

    private int Record(TypedRecordOptions typedOptions)
    {
        _ = typedOptions.MidiInputs.Iter(input => _logger.LogInformation("Using MIDI input {MidiInputId} ({MidiInputName})", input.Id, input.Name));
        var source = _midiTools.BuildMidiSource(typedOptions.MidiInputs);
        PrintOptions(typedOptions, _logger);
        var allEvents = source.AllEvents;

        var split = MidiSplitter.Split(allEvents, typedOptions.TimeoutToSave, typedOptions.DelayToSave);

        allEvents.ForEachAsync(e => _logger.LogTrace("{MidiEvent}", e));
        Task? dumpTask = null;
        if (typedOptions.DumpFile)
        {
            var dumpFileName = DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".txt";
            dumpTask = allEvents
                .Buffer(10)
                .ForEachAsync(batch =>
                {
                    _logger.LogTrace("Dumping {Count} events to file {DumpFile}", batch.Count, dumpFileName);
                    using var writer = new StreamWriter(dumpFileName);
                    foreach (var line in batch) writer.WriteLine(line);
                });
        }

        _ = split.AdjustedReleaseMarkers.ForEachAsync(_ => _logger.LogTrace("All Notes/Pedals Off!"));
        _ = split.ExtraOffEvents.ForEachAsync(e => _logger.LogTrace("{Event} (Introduced because of Held Events Timeout)", e));
        _ = split.SplitGroups.SelectMany(x => x.ToArray()
                .Where(midiEvents => midiEvents.Length > 0)
                .Select(midiEvents => BuildMidiFile(midiEvents, typedOptions.PathFormatString)))
                .ForEachAsync(x =>
                {
                    _logger.LogInformation("Saving {EventCount} events to file {FilePath}...", x.Tracks.Sum(y => y.Count()) - x.Tracks.Count(), x.FilePath);
                    try
                    {
                        _midiTools.SaveFile(x.Tracks, x.FilePath, typedOptions.MidiResolution);
                    }
#pragma warning disable CA1031
                    catch (Exception ex)
#pragma warning restore CA1031
                    {
                        _logger.LogError(ex, "There was an error when saving the file");
                    }
                });


        source.StartReceiving();

        _logger.LogInformation("Recording started, Press any key to quit");
        Console.ReadLine();
        source.Dispose();
        dumpTask?.Wait();
        return 0;
    }

    private MidiFileInfo<TMidiEvent> BuildMidiFile(IEnumerable<TMidiEvent> midiEvents, string pathFormatString)
    {
        var eventsArray = midiEvents as TMidiEvent[] ?? midiEvents.ToArray();
        var filePath = MidiFileContext.BuildFilePath(
            pathFormatString,
            eventsArray,
            DateTime.Now,
            Guid.NewGuid());
        var tracks = _midiTools.BuildTracks(eventsArray);
        return new MidiFileInfo<TMidiEvent>(tracks, filePath);
    }

    private static void PrintOptions(TypedRecordOptions options, ILogger logger)
    {
        (TimeSpan timeToSaveAfterAllOff, TimeSpan timeToSaveAfterHeldEvents, var pathFormatString, var midiResolution, _, _) = options;
#pragma warning disable CA1848
        logger.LogInformation("Working dir: {CurrentDirectory}", Environment.CurrentDirectory);
        logger.LogInformation("Delay to save after all notes off: {DelayToSave}", timeToSaveAfterAllOff);
        logger.LogInformation("Held events timeout: {TimeoutToSave}", timeToSaveAfterHeldEvents);
        logger.LogInformation("Output path: {PathFormatString}", pathFormatString);
        logger.LogInformation("MIDI resolution: {MidiResolution}", midiResolution);
    }
}
