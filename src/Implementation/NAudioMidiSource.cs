using System.Reactive.Linq;
using MidiRecorder.Application.Record;
using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

internal class NAudioMidiSource : IMidiSource<NAudioMidiEvent>
{
    private readonly MidiIn[] _midiIns;
    private readonly CancellationTokenSource _cts = new();

    public NAudioMidiSource(IEnumerable<MidiInput> midiInputs)
    {
        var ct = _cts.Token;
        var q = midiInputs.Select(
                input =>
                {
                    var midiIn = new MidiIn(input.Id);
                    var observable = Observable.FromEventPattern<MidiInMessageEventArgs>(
                            a => midiIn.MessageReceived += a,
                            a => midiIn.MessageReceived -= a)
                        .TakeUntil(ct)
                        .Select(x => x.EventArgs)
                        .Select(
                            e =>
                            {
                                MidiEvent? eventClone = e.MidiEvent.Clone();
                                eventClone.AbsoluteTime = e.Timestamp;
                                if (eventClone is NoteOnEvent non)
                                {
                                    non.NoteLength = 0;
                                }

                                return new NAudioMidiEvent(eventClone, input.Id);
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

    public IObservable<NAudioMidiEvent> AllEvents { get; }

    public void Dispose()
    {
        foreach (MidiIn midiIn in _midiIns)
        {
            midiIn.Stop();
            midiIn.Dispose();
        }
        _cts.Cancel();
        _cts.Dispose();
    }
}
