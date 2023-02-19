using System.Reactive.Linq;
using Microsoft.Extensions.Logging;

namespace MidiRecorder.Application;

public class MidiRecorderApplicationService<TMidiEvent>
{
    private readonly IMidiEventAnalyzer<TMidiEvent> _analyzer;
    private readonly IMidiFileSaver<TMidiEvent> _fileSaver;
    private readonly IFormatTester _formatTester;
    private readonly ILogger<MidiRecorderApplicationService<TMidiEvent>> _logger;
    private readonly IMidiSourceBuilder<TMidiEvent> _sourceBuilder;
    private readonly IMidiSplitter<TMidiEvent> _splitter;
    private readonly IMidiTrackBuilder<TMidiEvent> _trackBuilder;

    public MidiRecorderApplicationService(
        IMidiSourceBuilder<TMidiEvent> sourceBuilder,
        ILogger<MidiRecorderApplicationService<TMidiEvent>> logger,
        IMidiFileSaver<TMidiEvent> fileSaver,
        IMidiEventAnalyzer<TMidiEvent> analyzer,
        IMidiSplitter<TMidiEvent> splitter,
        IMidiTrackBuilder<TMidiEvent> trackBuilder,
        IFormatTester formatTester)
    {
        _sourceBuilder = sourceBuilder;
        _logger = logger;
        _fileSaver = fileSaver;
        _analyzer = analyzer;
        _splitter = splitter;
        _trackBuilder = trackBuilder;
        _formatTester = formatTester;
    }

    public IDisposable? StartRecording(TypedRecordOptions options)
    {
        PrintOptions(options);

        (TimeSpan delayToSave, TimeSpan timeoutToSave, var pathFormatString, var midiResolution, _) = options;
        if (!_formatTester.TestFormat(pathFormatString))
        {
            return null;
        }

        var source = _sourceBuilder.Build(options);


        var allEvents = source.AllEvents;
        var split = _splitter.Split(allEvents, _analyzer.NoteAndSustainPedalCount, timeoutToSave, delayToSave);
        _ = allEvents.ForEachAsync(e => _logger.LogTrace("{MidiEvent}", e));
        _ = split.AdjustedReleaseMarkers.ForEachAsync(_ => _logger.LogTrace("All Notes/Pedals Off!"));
        _ = split.SplitGroups.ForEachAsync(x => x.ToArray().ForEachAsync(SaveMidiFile));

        source.StartReceiving();
        return source;

        void SaveMidiFile(IEnumerable<TMidiEvent> eventList)
        {
            var midiEvents = eventList as TMidiEvent[] ?? eventList.ToArray();
            var context = new MidiFileContext<TMidiEvent>(midiEvents, DateTime.Now, Guid.NewGuid(), _analyzer);
            var filePath = context.BuildFilePath(pathFormatString);
            _logger.LogInformation("Saving {EventCount} events to file {FilePath}...", midiEvents.Length, filePath);
            try
            {
                var tracks = _trackBuilder.BuildTracks(midiEvents);
                _fileSaver.Save(tracks, filePath, midiResolution);
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(ex, "There was an error when saving the file");
            }
        }
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
