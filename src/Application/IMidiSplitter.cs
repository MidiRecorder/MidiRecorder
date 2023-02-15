namespace MidiRecorder.Application;

public interface IMidiSplitter<TMidiEvent>
{
    MidiSplit<TMidiEvent> Build(IObservable<TMidiEvent> allEvents, Func<TMidiEvent, int> noteAndSustainPedalCount,
        TimeSpan timeoutToSave, TimeSpan delayToSave);
}
