using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using CannedBytes.Midi;
using CannedBytes.Midi.IO;
using MidiRecorder.CommandLine;

namespace MidiRecorder
{
    public class MidiRecorderApplicationService
    {
        public RecordResult StartRecording(RecordOptions options)
        {
            var inputId = GetMidiInputId(options.MidiInputName);
            if (inputId == null)
            {
                return new RecordResult($"The MIDI input '{options.MidiInputName}' could not be located");
            }

            var delayToSave = TimeSpan.FromMilliseconds(options.DelayToSave);
            var pathFormatString = options.PathFormatString;

            var midiIn = new MidiInPort();
            var receiver = new ObservableReceiver();

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
                Console.WriteLine($"SAVING {eventList.Count} EVENTS TO FILE {filePath}...");
                MidiFileSerializer.Serialize(eventList, filePath);
            }
        }

        static int? GetMidiInputId(string midiInputName)
        {
            var midiInCapabilities = new MidiInPortCapsCollection();

            if (midiInCapabilities.Count == 0)
            {
                return null;
            }

            int? selectedIdx = midiInCapabilities
                .Select((port, idx) => new {port, idx})
                .FirstOrDefault(x => string.Equals(x.port.Name, midiInputName, StringComparison.OrdinalIgnoreCase))
                ?.idx;

            if (selectedIdx == null)
            {
                return null;
            }

            Console.WriteLine("The chosen MIDI input is " + midiInCapabilities[selectedIdx.Value].Name);
            return selectedIdx.Value;
        }
    }
}
