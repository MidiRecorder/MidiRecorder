namespace MidiRecorder.Application;

public record TypedRecordOptions(
    TimeSpan DelayToSave, TimeSpan TimeoutToSave, string PathFormatString,
    int MidiResolution, IEnumerable<int> MidiInputs)
{
    public override string ToString()
    {
        return $"{{ delayToSave = {DelayToSave}, timeoutToSave = {TimeoutToSave}, pathFormatString = {PathFormatString}, midiResolution = {MidiResolution} }}";
    }
}