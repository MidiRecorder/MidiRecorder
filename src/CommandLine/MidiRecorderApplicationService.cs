using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using CannedBytes.Midi;
using CannedBytes.Midi.IO;
using CannedBytes.Midi.Message;
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

        public RecordStartResult StartRecording(RecordOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            int[] inputIds = options.MidiInputs.SelectMany(GetMidiInputId).Distinct().ToArray();
            if (inputIds.Length == 0)
            {
                return new RecordStartResult($"No MIDI inputs for '{string.Join(", ", options.MidiInputs)}' could be located");
            }

            _logger.LogInformation($"Working dir: {Environment.CurrentDirectory}");
            var delayToSave = TimeSpan.FromMilliseconds(options.DelayToSave);
            _logger.LogInformation($"Delay to save: {delayToSave}");
            var timeoutToSave = TimeSpan.FromMilliseconds(30000);
            _logger.LogInformation($"Timeout to save: {timeoutToSave}");
            var pathFormatString = options.PathFormatString;
            _logger.LogInformation($"Output Path: {pathFormatString}");
            var midiResolution = options.MidiResolution;
            _logger.LogInformation($"MIDI resolution: {midiResolution}");

            var allEvents = new ObservableReceiverFactory(_logger);

            var timeouts = allEvents.Throttle(timeoutToSave);

            // How many notes+sust.pedal are held?
            var heldNotesAndPedals = allEvents
                .Select(ConvertNoteEventToNumber)
                .Where(x => x != 0)
                .Scan(0, (acum, n) => Math.Max(0, acum + n));

            // Finds a 
            var timeoutSavingPoints = heldNotesAndPedals
                .Throttle(timeoutToSave)
                .Where(x => x > 0)
                .Select(_ => 0);

            heldNotesAndPedals = heldNotesAndPedals
                .Merge(timeoutSavingPoints)
                .DistinctUntilChanged();

            var globalReleaseEvents = heldNotesAndPedals.Where(x => x == 0);

            _ = globalReleaseEvents.ForEachAsync(x => _logger.LogTrace("All Notes/Pedals Off!"));

            var globalReleaseSavingPoints = globalReleaseEvents.Throttle(delayToSave);

            var savingPoints = timeoutSavingPoints.Merge(globalReleaseSavingPoints);

            var groupsOfMidiFileEvents = allEvents
                .Window(savingPoints)
                .Select(x => x
                    .Aggregate(ImmutableList<MidiFileEvent>.Empty, (l, i) => l.Add(i)));

            _ = groupsOfMidiFileEvents
                .ForEachAsync(midiFileEvents => midiFileEvents
                    .ForEachAsync(SaveMidiFile));

            var midiIns = new List<MidiInPort>();

            foreach (var inputId in inputIds)
            {
                var midiIn = new MidiInPort
                {
                    Successor = allEvents.Build(inputId)
                };

                midiIn.Open(inputId);
                midiIn.Start();
                midiIns.Add(midiIn);
            }


            return new RecordStartResult(() =>
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
                _logger.LogInformation($"Saving {eventList.Count} events to file {filePath}...");
                try
                {
                    MidiFileSerializer.Serialize(eventList, filePath, midiResolution);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "There was an error when saving the file");
                }
            }
        }

        private static int ConvertNoteEventToNumber(MidiFileEvent arg)
        {
            return arg.Message switch
            {
                MidiChannelMessage message =>
                    message.Command switch
                    {
                        MidiChannelCommand.NoteOn => 1,
                        MidiChannelCommand.NoteOff => -1,
                        MidiChannelCommand.ControlChange when message.Parameter1 == 0x40 && message.Parameter2 == 0x7F => 1,
                        MidiChannelCommand.ControlChange when message.Parameter1 == 0x40 && message.Parameter2 == 0x00 => -1,
                        _ => 0
                    },
                _ => 0
            };
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
