using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

public class NAudioMidiSourceBuilder : IMidiSourceBuilder<MidiEvent>
{
    public IMidiSource<MidiEvent> Build(TypedRecordOptions typedOptions)
    {
        return new NAudioMidiSource(typedOptions);
    }
}
