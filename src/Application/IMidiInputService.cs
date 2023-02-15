namespace MidiRecorder.Application;

public interface IMidiInputService
{
    IEnumerable<int> GetMidiInputId(string midiInputName);
    IEnumerable<MidiInput> GetMidiInputs();
}
