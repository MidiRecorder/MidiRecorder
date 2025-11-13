namespace MidiRecorder.Application.Record;

public record TypedRecordOptions(
    TimeSpan DelayToSave,
    TimeSpan TimeoutToSave,
    string PathFormatString,
    int MidiResolution,
    bool DumpFile,
    IEnumerable<MidiInput> MidiInputs)
{
    public override string ToString()
    {
        return
            $"{{ timeToSaveAfterAllOff = {DelayToSave}, timeToSaveAfterHeldEvents = {TimeoutToSave}, pathFormatString = {PathFormatString}, midiResolution = {MidiResolution} }}";
    }
}
