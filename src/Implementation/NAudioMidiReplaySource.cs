using System.Reactive.Subjects;
using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

/// <summary>
/// Replays MIDI file events through the same <see cref="IMidiSource{MidiEventWithPort}"/> surface as live input.
/// </summary>
public sealed class NAudioMidiReplaySource : IMidiSource<MidiEventWithPort>
{
    private readonly Subject<MidiEventWithPort> _subject = new();
    private readonly IReadOnlyList<(TimeSpan Delay, MidiEventWithPort Event)> _steps;
    private CancellationTokenSource? _cts;
    private Task? _pump;

    public NAudioMidiReplaySource(string midiFilePath, bool realtime)
    {
        _steps = BuildReplaySteps(midiFilePath, realtime);
    }

    public IObservable<MidiEventWithPort> AllEvents => _subject;

    public void StartReceiving()
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _pump = Task.Run(
            async () =>
            {
                try
                {
                    foreach (var (delay, ev) in _steps)
                    {
                        if (delay > TimeSpan.Zero)
                        {
                            await Task.Delay(delay, token).ConfigureAwait(false);
                        }

                        _subject.OnNext(ev);
                    }

                    _subject.OnCompleted();
                }
                catch (OperationCanceledException)
                {
                    _subject.OnCompleted();
                }
            },
            token);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _subject.Dispose();
    }

    private static IReadOnlyList<(TimeSpan Delay, MidiEventWithPort Event)> BuildReplaySteps(
        string midiFilePath,
        bool realtime)
    {
        var midiFile = new MidiFile(midiFilePath, strictChecking: false);
        var ppqn = midiFile.DeltaTicksPerQuarterNote;
        var items = new List<(long Tick, int Port, MidiEvent Ev, bool IsTempo)>();

        for (var trackIndex = 0; trackIndex < midiFile.Tracks; trackIndex++)
        {
            var trackEvents = midiFile.Events[trackIndex];
            if (IsConductorTrack(trackEvents))
            {
                foreach (var midiEvent in trackEvents)
                {
                    if (midiEvent is TempoEvent)
                    {
                        items.Add((midiEvent.AbsoluteTime, 0, midiEvent, IsTempo: true));
                    }
                }

                continue;
            }

            var port = ResolvePort(trackEvents, trackIndex);
            foreach (var midiEvent in trackEvents)
            {
                if (midiEvent is TempoEvent)
                {
                    items.Add((midiEvent.AbsoluteTime, port, midiEvent, IsTempo: true));
                    continue;
                }

                if (midiEvent.CommandCode == MidiCommandCode.MetaEvent)
                {
                    continue;
                }

                items.Add((midiEvent.AbsoluteTime, port, midiEvent, IsTempo: false));
            }
        }

        var ordered = items
            .OrderBy(x => x.Tick)
            .ThenBy(x => x.IsTempo ? 0 : 1)
            .ThenBy(x => x.Port)
            .ToArray();

        var tempoUs = NAudioRawMidiCaptureWriter.DefaultMicrosecondsPerQuarter;
        var steps = new List<(TimeSpan Delay, MidiEventWithPort Event)>();
        long prevTick = 0;

        foreach (var (tick, port, ev, isTempo) in ordered)
        {
            if (isTempo && ev is TempoEvent te)
            {
                tempoUs = te.MicrosecondsPerQuarterNote;
                continue;
            }

            TimeSpan delay = TimeSpan.Zero;
            if (realtime)
            {
                var deltaTicks = Math.Max(0, tick - prevTick);
                delay = TicksToDelay(deltaTicks, ppqn, tempoUs);
                prevTick = tick;
            }

            var clone = ev.Clone();
            clone.AbsoluteTime = tick;
            steps.Add((delay, new MidiEventWithPort(clone, port)));
        }

        return steps;
    }

    private static bool IsConductorTrack(IList<MidiEvent> trackEvents) =>
        trackEvents.OfType<TextEvent>()
            .Any(
                te => te.MetaEventType == MetaEventType.SequenceTrackName
                      && string.Equals(te.Text, NAudioRawMidiCaptureWriter.ConductorTrackName, StringComparison.Ordinal));

    private static int ResolvePort(IList<MidiEvent> trackEvents, int trackIndex)
    {
        foreach (var e in trackEvents.OrderBy(x => x.AbsoluteTime))
        {
            if (e is TextEvent { MetaEventType: MetaEventType.SequenceTrackName } te
                && te.Text.StartsWith(NAudioRawMidiCaptureWriter.PortTrackPrefix, StringComparison.Ordinal)
                && int.TryParse(te.Text.AsSpan(NAudioRawMidiCaptureWriter.PortTrackPrefix.Length), out var p))
            {
                return p;
            }
        }

        return trackIndex;
    }

    private static TimeSpan TicksToDelay(long deltaTicks, int ppqn, int tempoUs)
    {
        if (deltaTicks <= 0)
        {
            return TimeSpan.Zero;
        }

        var ms = deltaTicks * (tempoUs / 1000.0) / ppqn;
        return TimeSpan.FromMilliseconds(ms);
    }
}
