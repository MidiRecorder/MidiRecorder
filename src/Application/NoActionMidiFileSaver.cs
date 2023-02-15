namespace MidiRecorder.Application;

public class NoActionMidiFileSaver<TMidiEvent> : IMidiFileSaver<TMidiEvent>
{
    public void Save(IEnumerable<TMidiEvent> events, string filePath, int timeDivision)
    {
        Console.WriteLine("FAKE SAVE: " + filePath);
    }
}
