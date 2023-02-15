using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

public class NAudioMidiEventAnalyzer : IMidiEventAnalyzer<MidiEventWithPort>
{
    public int NoteAndSustainPedalCount(MidiEventWithPort midiEvent)
    {
        return midiEvent.MidiEvent switch
        {
            NoteEvent { Velocity: > 0 } => 1,
            NoteEvent { Velocity: 0 } => -1,
            ControlChangeEvent { Controller: MidiController.Sustain, ControllerValue: 255 } => 1,
            ControlChangeEvent { Controller: MidiController.Sustain, ControllerValue: 0 } => -1,
            _ => 0
        };
    }

    public bool IsNote(MidiEventWithPort midiEvent)
    {
        return midiEvent is NoteEvent;
    }
}
