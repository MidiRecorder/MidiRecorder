using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

public static class NAudioMidiTrackBuilder
{
    public static IEnumerable<IEnumerable<MidiEventWithPort>> BuildTracks(IEnumerable<MidiEventWithPort> midiEvents)
    {
        var events = midiEvents.ToArray();
        // TODO: Failing test for empty midiEvents
        var firstTime = events[0].MidiEvent.AbsoluteTime;
        foreach (MidiEventWithPort midiEvent in events)
        {
            midiEvent.MidiEvent.AbsoluteTime -= firstTime;
        }

        return from midiEvent in events
            group midiEvent by (midiEvent.Port, midiEvent.MidiEvent.Channel)
            into trackGroups
            orderby trackGroups.Key
            let x = trackGroups.Concat(new[] { EndOfTrackMarker(trackGroups) })
            select x;
    }

    private static MidiEventWithPort EndOfTrackMarker(IEnumerable<MidiEventWithPort> track)
    {
        return new MidiEventWithPort(
            new MetaEvent(MetaEventType.EndTrack, 0, track.Last().MidiEvent.AbsoluteTime + 1),
            0);
    }
}
