namespace MidiRecorder.Application.Implementation;

public sealed class NAudioConfigurableMidiSourceBuilder : IMidiSourceBuilder<MidiEventWithPort>
{
    public IMidiSource<MidiEventWithPort> Build(TypedRecordOptions typedOptions)
    {
        IMidiSource<MidiEventWithPort> inner = string.IsNullOrEmpty(typedOptions.ReplayMidiPath)
            ? new NAudioMidiSource(typedOptions)
            : new NAudioMidiReplaySource(typedOptions.ReplayMidiPath!, typedOptions.ReplayRealtime);

        if (!string.IsNullOrEmpty(typedOptions.RawCapturePath))
        {
            inner = new RawMidiCaptureTappingSource(inner, typedOptions.RawCapturePath!, typedOptions.MidiResolution);
        }

        return inner;
    }
}
