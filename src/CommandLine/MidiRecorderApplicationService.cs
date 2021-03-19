using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using CannedBytes.Midi;
using CannedBytes.Midi.IO;
using Microsoft.Extensions.Logging;

namespace MidiRecorder.CommandLine
{
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
            var inputId = GetMidiInputId(options.MidiInput);
            if (inputId == null)
            {
                return new RecordResult($"The MIDI input '{options.MidiInput}' could not be located");
            }

            _logger.LogInformation("Working dir: " + Environment.CurrentDirectory);
            _logger.LogInformation("Output Path: " + options.PathFormatString);
            var delayToSave = TimeSpan.FromMilliseconds(options.DelayToSave);
            _logger.LogInformation("Delay to save: " + delayToSave);
            var pathFormatString = options.PathFormatString;

#pragma warning disable CA2000 // Dispose objects before losing scope -- The object will be disposed when the RecordResult is.
            var midiIn = new MidiInPort();
#pragma warning restore CA2000 // Dispose objects before losing scope
            var receiver = new ObservableReceiver(_logger);

            midiIn.Successor = receiver;

            var savingPoints = receiver
                .Throttle(delayToSave)
                .Select(x => x.AbsoluteTime);

            var windowed = receiver
                .Window(savingPoints)
                .Select(x => x
                    .Aggregate(ImmutableList<MidiFileEvent>.Empty, (l, i) => l.Add(i)))
                .ForEachAsync(midiFile => midiFile
                    .ForEachAsync(SaveMidiFile));

            midiIn.Open(inputId.Value);
            midiIn.Start();

            return new RecordResult(() =>
            {
                midiIn.Stop();
                midiIn.Dispose();
            });

            void SaveMidiFile(ImmutableList<MidiFileEvent> eventList)
            {
                var context = new MidiFileContext(eventList, DateTime.Now, Guid.NewGuid());
                string filePath = context.BuildFilePath(pathFormatString);
                _logger.LogInformation($"SAVING {eventList.Count} EVENTS TO FILE {filePath}...");
                MidiFileSerializer.Serialize(eventList, filePath);
            }
        }

        int? GetMidiInputId(string midiInputName)
        {
            var midiInCapabilities = new MidiInPortCapsCollection();

            if (midiInCapabilities.Count == 0)
            {
                _logger.LogWarning("You have no MIDI inputs");
                return null;
            }

            int? selectedIdx = null;
            if (int.TryParse(midiInputName, out var s))
            {
                selectedIdx = s >= 0 && s < midiInCapabilities.Count ? s : null;
            }

            selectedIdx ??= midiInCapabilities
                .Select((port, idx) => new {port, idx})
                .FirstOrDefault(x => string.Equals(x.port.Name, midiInputName, StringComparison.OrdinalIgnoreCase))
                ?.idx;

            if (selectedIdx == null)
            {
                _logger.LogWarning("MIDI input not found");
                return null;
            }

            _logger.LogInformation("MIDI Input: " + midiInCapabilities[selectedIdx.Value].Name);
            return selectedIdx.Value;
        }
    }
}
