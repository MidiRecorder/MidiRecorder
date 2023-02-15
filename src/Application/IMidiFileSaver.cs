namespace MidiRecorder.Application;

public interface IMidiFileSaver<in TMidiEvent>
{
    void Save(IEnumerable<IEnumerable<TMidiEvent>> events, string filePath, int timeDivision);
}
