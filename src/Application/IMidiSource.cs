namespace MidiRecorder.Application;

public interface IMidiSource<TMidiEvent> : IDisposable
{
    IObservable<TMidiEvent> AllEvents { get; }
    void StartReceiving();
}
