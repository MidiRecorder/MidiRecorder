namespace MidiRecorder.Application;

public interface IMidiSource<TMidiEvent> : IDisposable
{
    void StartReceiving();
    IObservable<TMidiEvent> AllEvents { get; }
}

public interface IMidiSourceBuilder<TMidiEvent>
{
    IMidiSource<TMidiEvent> Build(TypedRecordOptions typedOptions);
}
