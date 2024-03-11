using System.Reactive.Linq;
using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

public class NAudioMidiSource : IMidiSource<MidiEventWithPort>
{
    private readonly MidiIn[] _midiIns;

    public NAudioMidiSource(TypedRecordOptions typedOptions)
    {
        var q = typedOptions.MidiInputs.Select(
                input =>
                {
                    var midiIn = new MidiIn(input.id);
                    var observable = Observable.FromEventPattern<MidiInMessageEventArgs>(
                            a => midiIn.MessageReceived += a,
                            a => midiIn.MessageReceived -= a)
                        .Select(x => x.EventArgs)
                        .Select(
                            e =>
                            {
                                MidiEvent? eventClone = e.MidiEvent.Clone();
                                eventClone.AbsoluteTime = e.Timestamp;
                                return new MidiEventWithPort(eventClone, input.id);
                            });
                    return (midiIn, observable);
                })
            .ToArray();

        _midiIns = q.Select(x => x.midiIn).ToArray();
        AllEvents = q.Select(x => x.observable).Merge();
    }

    public void StartReceiving()
    {
        foreach (MidiIn midiIn in _midiIns)
        {
            midiIn.Start();
        }
    }

    public IObservable<MidiEventWithPort> AllEvents { get; }

    public void Dispose()
    {
        foreach (MidiIn midiIn in _midiIns)
        {
            midiIn.Stop();
            midiIn.Dispose();
        }
    }
}
