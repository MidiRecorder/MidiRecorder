namespace MidiRecorder.Application.Record;

public interface IMidiSource<TMidiEvent> : IDisposable
{
    void StartReceiving();
    IObservable<TMidiEvent> AllEvents { get; }
}
