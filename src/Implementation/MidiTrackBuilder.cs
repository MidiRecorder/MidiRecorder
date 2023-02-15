using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

internal sealed class MidiTrackBuilder
{
    private readonly IEnumerable<MidiEvent> _events;

    public MidiTrackBuilder(IEnumerable<MidiEvent> events)
    {
        _events = events;
    }

    public IEnumerable<IEnumerable<MidiEvent>> BuildTracks()
    {
        return from fileEvent in _events
            group fileEvent by fileEvent.Channel
            into trackGroups
            orderby trackGroups.Key
            let x = trackGroups.Concat(new[] { EndOfTrackMarker(trackGroups) })
            select x;
        //
        // IEnumerable<MidiEvent> NewFunction(IEnumerable<MidiEvent> track)
        // {
        //     MidiEvent? lastEvent = null;
        //
        //     foreach (var midiEvent in track)
        //     {
        //         if (lastEvent != null)
        //         {
        //             yield return new MidiEvent() .DeltaTime with { DeltaTime = midiEvent.AbsoluteTime - lastEvent.AbsoluteTime };
        //         }
        //         else
        //         {
        //             yield return midiEvent with { DeltaTime = 0 };
        //         }
        //
        //         lastEvent = midiEvent;
        //     }
        // }
    }

    private static MidiEvent EndOfTrackMarker(IEnumerable<MidiEvent> track)
    {
        return new MetaEvent(MetaEventType.EndTrack, 0, track.Last().AbsoluteTime + 1);
    }
}
