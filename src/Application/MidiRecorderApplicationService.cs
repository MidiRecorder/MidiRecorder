using System.Collections.Immutable;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;

namespace MidiRecorder.Application;

public class MidiRecorderApplicationService<TMidiEvent>
{
    private readonly IMidiEventAnalyzer<TMidiEvent> _analyzer;
    private readonly IMidiFileSaver<TMidiEvent> _fileSaver;
    private readonly ILogger<MidiRecorderApplicationService<TMidiEvent>> _logger;
    private readonly IMidiSourceBuilder<TMidiEvent> _sourceBuilder;
    private readonly IMidiSplitter<TMidiEvent> _splitter;

    public MidiRecorderApplicationService(IMidiSourceBuilder<TMidiEvent> sourceBuilder,
        ILogger<MidiRecorderApplicationService<TMidiEvent>> logger, IMidiFileSaver<TMidiEvent> fileSaver,
        IMidiEventAnalyzer<TMidiEvent> analyzer, IMidiSplitter<TMidiEvent> splitter)
    {
        _sourceBuilder = sourceBuilder;
        _logger = logger;
        _fileSaver = fileSaver;
        _analyzer = analyzer;
        _splitter = splitter;
    }

    public IDisposable StartRecording(TypedRecordOptions options)
    {
        PrintOptions(options);

        var source = _sourceBuilder.Build(options);

        (TimeSpan delayToSave, TimeSpan timeoutToSave, var pathFormatString, var midiResolution, _) = options;

        void SaveMidiFile(IEnumerable<TMidiEvent> eventList)
        {
            var midiEvents = eventList as TMidiEvent[] ?? eventList.ToArray();
            var context = new MidiFileContext<TMidiEvent>(midiEvents, DateTime.Now, Guid.NewGuid(), _analyzer);
            var filePath = context.BuildFilePath(pathFormatString);
            _logger.LogInformation("Saving {EventCount} events to file {FilePath}...", midiEvents.Length, filePath);
            try
            {
                _fileSaver.Save(midiEvents, filePath, midiResolution);
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(ex, "There was an error when saving the file");
            }
        }

        var allEvents = source.AllEvents;
        var x = new MidiSplitter<TMidiEvent>();
        var helper = x.Build(allEvents, _analyzer.NoteAndSustainPedalCount, timeoutToSave, delayToSave);
        _ = allEvents.ForEachAsync(e => _logger.LogTrace("{MidiEvent}", e));
        _ = helper.AdjustedReleaseMarkers.ForEachAsync(_ => _logger.LogTrace("All Notes/Pedals Off!"));

        _ = helper.SplitGroups.Select(x => x.Aggregate(ImmutableList<TMidiEvent>.Empty, (l, i) => l.Add(i)))
            .ForEachAsync(e => e.ForEachAsync(SaveMidiFile));

        source.StartReceiving();
        return source;
    }

    private void PrintOptions(TypedRecordOptions options)
    {
        (TimeSpan delayToSave, TimeSpan timeoutToSave, var pathFormatString, var midiResolution, _) = options;
#pragma warning disable CA1848
        _logger.LogInformation("Working dir: {CurrentDirectory}", Environment.CurrentDirectory);
        _logger.LogInformation("Delay to save: {DelayToSave}", delayToSave);
        _logger.LogInformation("Timeout to save: {TimeoutToSave}", timeoutToSave);
        _logger.LogInformation("Output Path: {PathFormatString}", pathFormatString);
        _logger.LogInformation("MIDI resolution: {MidiResolution}", midiResolution);
    }
}
