namespace MidiRecorder.Application;

public class MidiFileContext<TMidiEvent>
{
    private readonly IMidiEventAnalyzer<TMidiEvent> _analyzer;
    private readonly IEnumerable<TMidiEvent> _eventList;
    private readonly DateTime _now;
    private readonly Guid _uniqueIdentifier;

    public MidiFileContext(IEnumerable<TMidiEvent> eventList, DateTime now, Guid uniqueIdentifier,
        IMidiEventAnalyzer<TMidiEvent> analyzer)
    {
        _eventList = eventList;
        _now = now;
        _uniqueIdentifier = uniqueIdentifier;
        _analyzer = analyzer;
    }

    internal string BuildFilePath(string formatString)
    {
        return StringExt.Format(
            formatString,
            new FormatData<TMidiEvent>(_now, _eventList, _uniqueIdentifier, _analyzer));
    }
}
