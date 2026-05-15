using System.Reactive.Linq;

namespace MidiRecorder.Application.Implementation;

/// <summary>
/// Wraps a MIDI source and, on dispose, exports everything observed on <see cref="IMidiSource{MidiEventWithPort}.AllEvents"/>
/// as a debug MIDI file (see <see cref="NAudioRawMidiCaptureWriter"/>).
/// </summary>
public sealed class RawMidiCaptureTappingSource : IMidiSource<MidiEventWithPort>
{
    private readonly IMidiSource<MidiEventWithPort> _inner;
    private readonly List<MidiEventWithPort> _buffer = new();
    private readonly object _bufferLock = new();
    private readonly string _rawCapturePath;
    private readonly int _midiResolution;

    public RawMidiCaptureTappingSource(
        IMidiSource<MidiEventWithPort> inner,
        string rawCapturePath,
        int midiResolution)
    {
        _inner = inner;
        _rawCapturePath = rawCapturePath;
        _midiResolution = midiResolution;
        AllEvents = _inner.AllEvents.Do(
            e =>
            {
                lock (_bufferLock)
                {
                    _buffer.Add(new MidiEventWithPort(e.MidiEvent.Clone(), e.Port));
                }
            });
    }

    public IObservable<MidiEventWithPort> AllEvents { get; }

    public void StartReceiving() => _inner.StartReceiving();

    public void Dispose()
    {
        List<MidiEventWithPort> snapshot;
        lock (_bufferLock)
        {
            snapshot = _buffer.ToList();
        }

        try
        {
            NAudioRawMidiCaptureWriter.Save(snapshot, _rawCapturePath, _midiResolution);
        }
        finally
        {
            _inner.Dispose();
        }
    }
}
