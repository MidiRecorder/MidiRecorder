using System.Collections.Immutable;
using System.Reactive.Linq;
using CannedBytes.Midi;
using CannedBytes.Midi.IO;
using Microsoft.Extensions.Logging;

namespace MidiRecorder.CommandLine;

public class MidiRecorderApplicationService
{
    private readonly ILogger _logger;

    public MidiRecorderApplicationService(ILogger logger)
    {
        _logger = logger;
    }

    public RecordResult StartRecording(RecordOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        int[] inputIds = options.MidiInputs.SelectMany(GetMidiInputId).Distinct().ToArray();
        if (inputIds.Length == 0)
        {
            return new RecordResult($"No MIDI inputs for '{string.Join(", ", options.MidiInputs)}' could be located");
        }

        _logger.LogInformation("Working dir: {CurrentDirectory}", Environment.CurrentDirectory);
        var delayToSave = TimeSpan.FromMilliseconds(options.DelayToSave);
        _logger.LogInformation("Delay to save: {DelayToSave}", delayToSave);
        var pathFormatString = options.PathFormatString;
        _logger.LogInformation("Output Path: {PathFormatString}", pathFormatString);
        var midiResolution = options.MidiResolution;
        _logger.LogInformation("MIDI resolution: {MidiResolution}", midiResolution);

        var receiverFactory = new ObservableReceiverFactory(_logger);
        var savingPoints = receiverFactory
            .Throttle(delayToSave)
            .Select(x => x.AbsoluteTime);

        _ = receiverFactory
            .Window(savingPoints)
            .Select(x => x
                .Aggregate(ImmutableList<MidiFileEvent>.Empty, (l, i) => l.Add(i)))
            .ForEachAsync(midiFile => midiFile
                .ForEachAsync(SaveMidiFile));

        var midiIns = new List<MidiInPort>();

        foreach (var inputId in inputIds)
        {
            var midiIn = new MidiInPort
            {
                Successor = receiverFactory.Build(inputId)
            };

            midiIn.Open(inputId);
            midiIn.Start();
            midiIns.Add(midiIn);
        }


        return new RecordResult(() =>
        {
            foreach (var midiIn in midiIns)
            {
                midiIn.Stop();
                midiIn.Dispose();
            }
        });

        void SaveMidiFile(ImmutableList<MidiFileEvent> eventList)
        {
            var context = new MidiFileContext(eventList, DateTime.Now, Guid.NewGuid());
            string filePath = context.BuildFilePath(pathFormatString);
            _logger.LogInformation("Saving {EventCount} events to file {FilePath}...", eventList.Count, filePath);
            try
            {
                MidiFileSerializer.Serialize(eventList, filePath, midiResolution);
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(ex, "There was an error when saving the file");
            }
        }
    }

    IEnumerable<int> GetMidiInputId(string midiInputName)
    {
        var midiInCapabilities = new MidiInPortCapsCollection().ToArray();

        if (midiInCapabilities.Length == 0)
        {
            _logger.LogWarning("You have no MIDI inputs");
            yield break;
        }

        if (midiInputName == "*")
        {
            foreach (int i in Enumerable.Range(0, midiInCapabilities.Length))
            {
                _logger.LogInformation("MIDI Input: {Name}", midiInCapabilities[i].Name);
                yield return i;
            }
            yield break;
        }

        int? selectedIdx = null;
        if (int.TryParse(midiInputName, out var s))
        {
            selectedIdx = s >= 0 && s < midiInCapabilities.Length ? s : null;
        }

        selectedIdx ??= midiInCapabilities
            .Select((port, idx) => new {port, idx})
            .FirstOrDefault(x => string.Equals(x.port.Name, midiInputName, StringComparison.OrdinalIgnoreCase))
            ?.idx;

        if (selectedIdx == null)
        {
            _logger.LogWarning("MIDI input not found");
            yield break;
        }

        _logger.LogInformation("MIDI Input: {Name}", midiInCapabilities[selectedIdx.Value].Name);
        yield return selectedIdx.Value;
    }
}
