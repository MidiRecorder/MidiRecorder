using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace MidiRecorder.Application;

public class MidiSplit<TMidiEvent>
{
    public MidiSplit(IObservable<int> heldNotesAndPedals, IObservable<char> heldNotesAndPedalsTimeoutMarkers,
        IObservable<int> adjustedHeldNotesAndPedals, IObservable<char> adjustedReleaseMarkers,
        IObservable<char> globalReleaseSavingPoints, IObservable<char> savingPoints,
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
    public IObservable<char> HeldNotesAndPedalsTimeoutMarkers { get; }
    public IObservable<int> AdjustedHeldNotesAndPedals { get; }
    public IObservable<char> AdjustedReleaseMarkers { get; }
    public IObservable<char> GlobalReleaseSavingPoints { get; }
    public IObservable<char> SavingPoints { get; }
    public IObservable<IObservable<TMidiEvent>> SplitGroups { get; }
}

public class MidiSplitter<TMidiEvent> : IMidiSplitter<TMidiEvent>
{
    private readonly IScheduler _scheduler;

    public MidiSplitter(IScheduler? scheduler = null)
    {
        _scheduler = scheduler ?? Scheduler.Default;
    }

    public MidiSplit<TMidiEvent> Build(IObservable<TMidiEvent> allEvents,
        Func<TMidiEvent, int> noteAndSustainPedalCount, TimeSpan timeoutToSave, TimeSpan delayToSave)
    {
        IScheduler scheduler1 = _scheduler ?? DefaultScheduler.Instance;
        const char marker = 'm';

        // How many notes + sust. pedal are held?
        var heldNotesAndPedals = allEvents.Select(noteAndSustainPedalCount).Where(x => x != 0)
            .Scan(0, (accum, n) => Math.Max(0, accum + n));

        // Finds points where a note or pedal is held for too long
        // (The marker is at the end of such a pause)
        var heldNotesAndPedalsTimeoutMarkers = heldNotesAndPedals.Throttle(timeoutToSave, scheduler1).Where(x => x > 0)
            .Select(_ => marker);

        // Introduce a zero in held notes and pedals where a note or pedal is held for too long
        var adjustedHeldNotesAndPedals = heldNotesAndPedals.Merge(heldNotesAndPedalsTimeoutMarkers.Select(_ => 0))
            .DistinctUntilChanged();

        // Release markers (after removing notes and pedals held for too long)
        var adjustedReleaseMarkers = adjustedHeldNotesAndPedals.Where(x => x == 0).Select(_ => marker);

        // Remove releases that are too close to each other
        var globalReleaseSavingPoints = adjustedReleaseMarkers.Throttle(delayToSave, scheduler1).Select(_ => marker);

        var savingPoints = heldNotesAndPedalsTimeoutMarkers.Merge(globalReleaseSavingPoints);

        var splitGroups = allEvents.Window(savingPoints);

        return new MidiSplit<TMidiEvent>(
            heldNotesAndPedals,
            heldNotesAndPedalsTimeoutMarkers,
            adjustedHeldNotesAndPedals,
            adjustedReleaseMarkers,
            globalReleaseSavingPoints,
            savingPoints,
            splitGroups);
    }
}
