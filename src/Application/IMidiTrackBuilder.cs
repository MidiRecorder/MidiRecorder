namespace MidiRecorder.Application;

public interface IMidiTrackBuilder<TMidiEvent>
{
    IEnumerable<IEnumerable<TMidiEvent>> BuildTracks(IEnumerable<TMidiEvent> midiEvents);
}
