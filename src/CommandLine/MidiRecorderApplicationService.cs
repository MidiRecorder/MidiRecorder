using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using CannedBytes.Midi;
using CannedBytes.Midi.IO;

namespace MidiRecorder
{
    public class MidiRecorderApplicationService
    {
        public IDisposable StartRecording(int inputPortIndex, TimeSpan delayToSave, string pathFormatString)
        {
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
                    .ForEachAsync(eventList =>
                    {
                        SaveFile(eventList);
                    }));

            midiIn.Open(inputPortIndex);
            midiIn.Start();
            // Console.WriteLine("Recording started. Press any key to quit.");
            // Console.ReadLine();

            return new Disposer(() => midiIn.Stop());

            void SaveFile(ImmutableList<MidiFileEvent> eventList)
            {
                var context = new MidiFileContext(eventList, DateTime.Now);
                string filePath = context.BuildFilePath(pathFormatString);
                Console.WriteLine($"SAVING {eventList.Count} EVENTS TO FILE {filePath}...");
                MidiFileSerializer.Serialize(eventList, filePath);
            }
        }

        private class Disposer : IDisposable
        {
            private readonly Action _action;

            public Disposer(Action action)
            {
                _action = action ?? throw new ArgumentNullException(nameof(action));
            }

            public void Dispose()
            {
                _action();
            }
        }
    }
}
