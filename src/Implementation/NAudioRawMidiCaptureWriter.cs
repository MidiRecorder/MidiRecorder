using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

/// <summary>
/// Writes a Type 1 MIDI file that preserves the merged input timeline: one conductor track (tempo map)
/// and one track per input port. Timing is derived from <see cref="MidiEvent.AbsoluteTime"/> on captured
/// events (NAudio driver timestamps, treated as milliseconds from session start).
/// </summary>
public static class NAudioRawMidiCaptureWriter
{
    public const int DefaultMicrosecondsPerQuarter = 500_000; // 120 BPM

    public const string ConductorTrackName = "MidiRecorderConductor";

    public const string PortTrackPrefix = "MidiRecorderPort:";

    public static void Save(
        IReadOnlyList<MidiEventWithPort> events,
        string filePath,
        int pulsesPerQuarterNote,
        int microsecondsPerQuarter = DefaultMicrosecondsPerQuarter)
    {
        if (events.Count == 0)
        {
            return;
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var t0 = events.Min(e => e.MidiEvent.AbsoluteTime);

        var collection = new MidiEventCollection(1, pulsesPerQuarterNote);

        var conductor = new List<MidiEvent>
        {
            new TempoEvent(microsecondsPerQuarter, 0),
            new TextEvent(ConductorTrackName, MetaEventType.SequenceTrackName, 0),
            new MetaEvent(MetaEventType.EndTrack, 0, 1)
        };
        collection.AddTrack(conductor);

        foreach (var portGroup in events.GroupBy(e => e.Port).OrderBy(g => g.Key))
        {
            var ordered = portGroup.OrderBy(e => e.MidiEvent.AbsoluteTime).ToArray();
            var track = new List<MidiEvent>
            {
                new TextEvent($"{PortTrackPrefix}{portGroup.Key}", MetaEventType.SequenceTrackName, 0)
            };

            foreach (var ev in ordered)
            {
                var msFromStart = ev.MidiEvent.AbsoluteTime - t0;
                var ticks = MsToTicks(msFromStart, pulsesPerQuarterNote, microsecondsPerQuarter);
                var clone = ev.MidiEvent.Clone();
                clone.AbsoluteTime = ticks;
                track.Add(clone);
            }

            var endTick = track[^1].AbsoluteTime + 1;
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, endTick));
            collection.AddTrack(track);
        }

        MidiFile.Export(filePath, collection);
    }

    private static long MsToTicks(long msFromStart, int ppqn, int microsecondsPerQuarter)
    {
        if (msFromStart <= 0)
        {
            return 0;
        }

        return msFromStart * ppqn * 1000L / microsecondsPerQuarter;
    }
}
