namespace MidiRecorder.Application;

public static class MidiFileContext
{
    public static string BuildFilePath<TMidiEvent>(
        string formatString,
        IEnumerable<TMidiEvent> eventList,
        DateTime now,
        Guid uniqueIdentifier,
        Func<TMidiEvent, bool> isNote) =>
        StringExt.Format(
            formatString,
            new FormatData<TMidiEvent>(now, eventList, uniqueIdentifier, isNote));
}
