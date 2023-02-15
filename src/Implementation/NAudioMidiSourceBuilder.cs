namespace MidiRecorder.Application.Implementation;

public class NAudioMidiSourceBuilder : IMidiSourceBuilder<MidiEventWithPort>
{
    public IMidiSource<MidiEventWithPort> Build(TypedRecordOptions typedOptions)
    {
        return new NAudioMidiSource(typedOptions);
    }
}
