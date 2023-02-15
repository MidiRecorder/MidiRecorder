namespace MidiRecorder.Application;

public interface IMidiEventAnalyzer<in TMidiEvent>
{
    int NoteAndSustainPedalCount(TMidiEvent midiEvent);
    bool IsNote(TMidiEvent midiEvent);
}
