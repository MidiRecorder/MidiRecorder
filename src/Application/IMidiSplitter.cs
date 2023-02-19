namespace MidiRecorder.Application;

public interface IMidiSplitter<TMidiEvent>
{
    MidiSplit<TMidiEvent> Split(
        IObservable<TMidiEvent> allEvents,
        Func<TMidiEvent, int> noteAndSustainPedalCount,
        TimeSpan timeoutToSave,
        TimeSpan delayToSave);
}
