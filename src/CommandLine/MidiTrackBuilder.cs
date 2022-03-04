using CannedBytes.Midi.IO;
using CannedBytes.Midi.Message;

namespace MidiRecorder.CommandLine;

internal sealed class MidiTrackBuilder
{
    private readonly IEnumerable<MidiFileEvent> _events;

    public MidiTrackBuilder(IEnumerable<MidiFileEvent> events)
    {
        _events = events;
    }

    public IEnumerable<IEnumerable<MidiFileEvent>> BuildTracks()
    {
        var result = from fileEvent in _events
            group fileEvent by (((MidiShortMessage)fileEvent.Message).Status & 0x0F) into trackGroups
            orderby trackGroups.Key
            select trackGroups.Concat(new[] { EndOfTrackMarker(trackGroups) });

        // fix the delta times
        foreach (var track in result)
        {
            MidiFileEvent? lastEvent = null;

            foreach (var fileEvent in track)
            {
                if (lastEvent != null)
                {
                    fileEvent.DeltaTime = fileEvent.AbsoluteTime - lastEvent.AbsoluteTime;
                }
                else
                {
                    fileEvent.DeltaTime = 0;
                }

                lastEvent = fileEvent;
            }
        }

        return result;
    }

    private static MidiFileEvent EndOfTrackMarker(IEnumerable<MidiFileEvent> track)
    {
        return new MidiFileEvent
        {
            Message = new MidiMetaMessage(MidiMetaType.EndOfTrack, Array.Empty<byte>()),
            AbsoluteTime = track.Last().AbsoluteTime + 1,
            DeltaTime = 1
        };
    }
}