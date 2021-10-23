using System;
using System.Linq;
using System.Reactive.Linq;
using CannedBytes.Midi;
using CannedBytes.Midi.IO;
using CannedBytes.Midi.Message;

namespace MidiRecorder
{
    public class ObservableReceiver : IMidiDataReceiver, IObservable<MidiFileEvent>
    {
        private readonly MidiMessageFactory _factory = new MidiMessageFactory();
        private readonly IObservable<MidiFileEvent> _observable;
        private event EventHandler<MidiFileEvent>? MidiEvent;

        public ObservableReceiver()
        {
            _observable = Observable.FromEventPattern<MidiFileEvent>(
                a => MidiEvent += a,
                a => MidiEvent -= a
            ).Select(x => x.EventArgs);
        }

        public void ShortData(int data, long timestamp)
        {
            Console.Write($"{timestamp}");
            var midiEvent = new MidiFileEvent
            {
                Message = _factory.CreateShortMessage(data),
                AbsoluteTime = timestamp
            };

            foreach (var b in midiEvent.Message.GetData())
            {
                Console.Write(" {0:X2}", b);
            }

            Console.WriteLine();
            MidiEvent?.Invoke(this, midiEvent);
        }

        public void LongData(MidiBufferStream buffer, long timestamp)
        {
            // not used for short midi messages
        }

        public IDisposable Subscribe(IObserver<MidiFileEvent> observer)
        {
            return _observable.Subscribe(observer);
        }
    }
}
