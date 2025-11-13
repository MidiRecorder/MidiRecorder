using MidiRecorder.Application.Record;

namespace MidiRecorder.Application;

public abstract class MidiTools<TMidiEvent> where TMidiEvent : IMidiEvent
{
    public abstract IEnumerable<MidiInput> GetMidiInputs();
    public abstract IEnumerable<MidiInput> SearchMidiInputId(string midiInputId);
    public abstract IMidiSource<TMidiEvent> BuildMidiSource(IEnumerable<MidiInput> midiInput);
    public abstract IEnumerable<IEnumerable<TMidiEvent>> BuildTracks(IEnumerable<TMidiEvent> events);
    public abstract void SaveFile(IEnumerable<IEnumerable<TMidiEvent>> tracks, string filePath, int timePrecision);
    public abstract IEnumerable<IEnumerable<TMidiEvent>> OpenFile(string filePath, int port);
}
