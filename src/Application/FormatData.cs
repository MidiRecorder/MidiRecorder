using System.Collections.Immutable;

namespace MidiRecorder.Application;

internal class FormatData<TMidiEvent>
{
    private readonly IMidiEventAnalyzer<TMidiEvent> _analyzer;
    private readonly IEnumerable<TMidiEvent> _eventList;
    private readonly Dictionary<string, object?> _memoStore = new();

    private T? Memoize<T>(string key, Func<T> expression)
    {
        if (!_memoStore.ContainsKey(key))
        {
            _memoStore.Add(key, expression());
        }

        return (T?)_memoStore[key];
    }

    public DateTime Now { get; }
    public int NumberOfEvents => _eventList.Count();
    public int NumberOfNoteEvents => Memoize(nameof(NumberOfNoteEvents), () => _eventList.Count(_analyzer.IsNote));
    public Guid Guid { get; }

    public FormatData(DateTime now, IEnumerable<TMidiEvent> eventList, Guid guid, IMidiEventAnalyzer<TMidiEvent> analyzer)
    {
        _eventList = eventList;
        Now = now;
        Guid = guid;
        _analyzer = analyzer;
    }

    public override bool Equals(object? obj)
    {
        return obj is FormatData<TMidiEvent> other &&
               Now == other.Now &&
               NumberOfEvents == other.NumberOfEvents &&
               Guid.Equals(other.Guid);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Now, NumberOfEvents, Guid);
    }
}
