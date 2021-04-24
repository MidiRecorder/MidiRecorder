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
    public class ObservableReceiverFactory : IObservable<MidiFileEvent>
    {
        private readonly ILogger _logger;
        private readonly IObservable<MidiFileEvent> _observable;
        private event EventHandler<MidiFileEvent>? MidiEvent;
        private readonly MidiMessageFactory _factory = new();

        public ObservableReceiverFactory(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _observable = Observable.FromEventPattern<MidiFileEvent>(
                a => MidiEvent += a,
                a => MidiEvent -= a
            ).Select(x => x.EventArgs);
        }

        public IMidiDataReceiver Build(int port)
        {
            return new FunctionMidiDataReceiver(port, ShortData);
        }

        public void ShortData(int data, long timestamp, int port)
        {
            var sb = new StringBuilder();

            _ = sb.Append($"{port} {timestamp} {data} ");
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

        public IDisposable Subscribe(IObserver<MidiFileEvent> observer)
        {
            return _observable.Subscribe(observer);
        }

        private class FunctionMidiDataReceiver : IMidiDataReceiver
        {
            private readonly int _port;
            private readonly Action<int, long, int> _actionWithPort;

            public FunctionMidiDataReceiver(int port, Action<int, long, int> actionWithPort)
            {
                _port = port;
                _actionWithPort = actionWithPort;
            }

            public void LongData(MidiBufferStream buffer, long timestamp)
            {
                // not used for short midi messages
            }

            public void ShortData(int data, long timestamp)
            {
                _actionWithPort(data, timestamp, _port);
            }
        }
    }
}
