namespace MidiRecorder.Application;

public record TypedRecordOptions(
    TimeSpan DelayToSave,
    TimeSpan TimeoutToSave,
    string PathFormatString,
    int MidiResolution,
    IEnumerable<int> MidiInputs,
    string? RawCapturePath = null,
    string? ReplayMidiPath = null,
    bool ReplayRealtime = false)
{
    public override string ToString()
    {
        return
            $"{{ delayToSave = {DelayToSave}, timeoutToSave = {TimeoutToSave}, pathFormatString = {PathFormatString}, midiResolution = {MidiResolution}, rawCapturePath = {RawCapturePath}, replayMidiPath = {ReplayMidiPath}, replayRealtime = {ReplayRealtime} }}";
    }
}
