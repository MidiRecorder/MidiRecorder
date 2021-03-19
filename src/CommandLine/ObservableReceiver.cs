using System;
using System.Globalization;
using System.Reactive.Linq;
using System.Text;
using CannedBytes.Midi;
using CannedBytes.Midi.IO;
using CannedBytes.Midi.Message;
using Microsoft.Extensions.Logging;

namespace MidiRecorder.CommandLine
{
    public class ObservableReceiver : IMidiDataReceiver, IObservable<MidiFileEvent>
    {
        private readonly ILogger _logger;
        private readonly MidiMessageFactory _factory = new MidiMessageFactory();
        private readonly IObservable<MidiFileEvent> _observable;
        private event EventHandler<MidiFileEvent>? MidiEvent;

        public ObservableReceiver(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _observable = Observable.FromEventPattern<MidiFileEvent>(
                a => MidiEvent += a,
                a => MidiEvent -= a
            ).Select(x => x.EventArgs);
        }

        public void ShortData(int data, long timestamp)
        {
            var sb = new StringBuilder();

            _ = sb.Append($"{timestamp}");
            var midiEvent = new MidiFileEvent
            {
                Message = _factory.CreateShortMessage(data),
                AbsoluteTime = timestamp
            };

            foreach (var b in midiEvent.Message.GetData())
            {
                _ = sb.AppendFormat(CultureInfo.InvariantCulture, " {0:X}", b);
            }
            _logger.LogTrace(sb.ToString());
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
