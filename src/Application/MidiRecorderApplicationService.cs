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
    private readonly object _lastSavedPathLock = new();
    private string? _lastSavedPath;
    private bool _lastSavedFileIsMarked;
    private string _markerSuffix = "_good";

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

    public bool TryMarkLastSavedFile(out string? markedPath)
    {
        markedPath = null;
        string? lastPath;
        var alreadyMarked = false;
        lock (_lastSavedPathLock)
        {
            lastPath = _lastSavedPath;
            alreadyMarked = _lastSavedFileIsMarked;
        }

        if (lastPath == null)
        {
            _logger.LogWarning("No saved file to mark yet.");
            return false;
        }

        if (alreadyMarked)
        {
            _logger.LogInformation("Last saved file is already marked in this session: {FilePath}", lastPath);
            markedPath = lastPath;
            return true;
        }

        if (!File.Exists(lastPath))
        {
            _logger.LogError("Cannot mark file because it no longer exists: {FilePath}", lastPath);
            return false;
        }

        markedPath = SavedFileMarker.ApplySuffix(lastPath, _markerSuffix);
        if (File.Exists(markedPath))
        {
            _logger.LogError("Cannot mark file because target already exists: {FilePath}", markedPath);
            markedPath = null;
            return false;
        }

        try
        {
            File.Move(lastPath, markedPath);
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogError(ex, "Failed to mark file {FilePath}", lastPath);
            markedPath = null;
            return false;
        }

        lock (_lastSavedPathLock)
        {
            _lastSavedPath = markedPath;
            _lastSavedFileIsMarked = true;
        }

        _logger.LogInformation("Marked {OldPath} -> {NewPath}", lastPath, markedPath);
        return true;
    }

    public IDisposable? StartRecording(TypedRecordOptions options)
    {
        PrintOptions(options);

        _markerSuffix = options.MarkerSuffix;
        lock (_lastSavedPathLock)
        {
            _lastSavedPath = null;
            _lastSavedFileIsMarked = false;
        }

        var delayToSave = options.DelayToSave;
        var timeoutToSave = options.TimeoutToSave;
        var pathFormatString = options.PathFormatString;
        var midiResolution = options.MidiResolution;
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
            if (midiEvents.Length == 0)
            {
                return;
            }

            var context = new MidiFileContext<TMidiEvent>(midiEvents, DateTime.Now, Guid.NewGuid(), _analyzer);
            var filePath = context.BuildFilePath(pathFormatString);
            _logger.LogInformation("Saving {EventCount} events to file {FilePath}...", midiEvents.Length, filePath);
            try
            {
                var tracks = _trackBuilder.BuildTracks(midiEvents);
                _fileSaver.Save(tracks, filePath, midiResolution);
                lock (_lastSavedPathLock)
                {
                    _lastSavedPath = filePath;
                    _lastSavedFileIsMarked = false;
                }
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
#pragma warning disable CA1848
        _logger.LogInformation("Working dir: {CurrentDirectory}", Environment.CurrentDirectory);
        _logger.LogInformation("Delay to save: {DelayToSave}", options.DelayToSave);
        _logger.LogInformation("Timeout to save: {TimeoutToSave}", options.TimeoutToSave);
        _logger.LogInformation("Output Path: {PathFormatString}", options.PathFormatString);
        _logger.LogInformation("MIDI resolution: {MidiResolution}", options.MidiResolution);
        _logger.LogInformation("Marker suffix: {MarkerSuffix}", options.MarkerSuffix);
        if (!string.IsNullOrEmpty(options.ReplayMidiPath))
        {
            _logger.LogInformation(
                "Replay from file: {ReplayPath} (realtime pacing: {ReplayRealtime})",
                options.ReplayMidiPath,
                options.ReplayRealtime);
        }

        if (!string.IsNullOrEmpty(options.RawCapturePath))
        {
            _logger.LogInformation("Raw capture file: {RawCapturePath}", options.RawCapturePath);
        }
#pragma warning restore CA1848
    }
}
