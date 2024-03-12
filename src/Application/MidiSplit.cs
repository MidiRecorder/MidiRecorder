using LanguageExt;

namespace MidiRecorder.Application;

public class MidiSplit<TMidiEvent>
{
    public MidiSplit(
        IObservable<int> heldNotesAndPedals,
        IObservable<Unit> heldNotesAndPedalsTimeoutMarkers,
        IObservable<int> adjustedHeldNotesAndPedals,
        IObservable<Unit> adjustedReleaseMarkers,
        IObservable<Unit> globalReleaseSavingPoints,
        IObservable<Unit> savingPoints,
        IObservable<IObservable<TMidiEvent>> splitGroups)
    {
        HeldNotesAndPedals = heldNotesAndPedals;
        HeldNotesAndPedalsTimeoutMarkers = heldNotesAndPedalsTimeoutMarkers;
        AdjustedHeldNotesAndPedals = adjustedHeldNotesAndPedals;
        AdjustedReleaseMarkers = adjustedReleaseMarkers;
        GlobalReleaseSavingPoints = globalReleaseSavingPoints;
        SavingPoints = savingPoints;
        SplitGroups = splitGroups;
    }

    public IObservable<int> HeldNotesAndPedals { get; }
    public IObservable<Unit> HeldNotesAndPedalsTimeoutMarkers { get; }
    public IObservable<int> AdjustedHeldNotesAndPedals { get; }
    public IObservable<Unit> AdjustedReleaseMarkers { get; }
    public IObservable<Unit> GlobalReleaseSavingPoints { get; }
    public IObservable<Unit> SavingPoints { get; }
    public IObservable<IObservable<TMidiEvent>> SplitGroups { get; }
}
