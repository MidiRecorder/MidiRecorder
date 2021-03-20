using System;
using System.Collections.Generic;
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
            int[] inputIds = options.MidiInputs.SelectMany(GetMidiInputId).Distinct().ToArray();
            if (inputIds.Length == 0)
            {
                return new RecordResult($"No MIDI inputs for '{string.Join(", ", options.MidiInputs)}' could be located");
            }

            _logger.LogInformation("Working dir: " + Environment.CurrentDirectory);
            _logger.LogInformation("Output Path: " + options.PathFormatString);
            var delayToSave = TimeSpan.FromMilliseconds(options.DelayToSave);
            _logger.LogInformation("Delay to save: " + delayToSave);
            var pathFormatString = options.PathFormatString;

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
#pragma warning disable CA2000 // Dispose objects before losing scope -- The object will be disposed when the RecordResult is.
                var midiIn = new MidiInPort
                {
                    Successor = receiverFactory.Build(inputId)
                };
#pragma warning restore CA2000 // Dispose objects before losing scope

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
                _logger.LogInformation($"SAVING {eventList.Count} EVENTS TO FILE {filePath}...");
                MidiFileSerializer.Serialize(eventList, filePath);
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
                    _logger.LogInformation("MIDI Input: " + midiInCapabilities[i].Name);
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

            _logger.LogInformation("MIDI Input: " + midiInCapabilities[selectedIdx.Value].Name);
            yield return selectedIdx.Value;
        }
    }
}
