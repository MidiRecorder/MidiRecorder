namespace MidiRecorder.Application;

public interface IMidiFileSaver<TMidiEvent>
{
    void Save(IEnumerable<TMidiEvent> events, string filePath, int timeDivision);
}
