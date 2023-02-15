using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

public class NAudioMidiEventAnalyzer : IMidiEventAnalyzer<MidiEvent>
{
    public int NoteAndSustainPedalCount(MidiEvent midiEvent)
    {
        return midiEvent switch
        {
            NoteEvent { Velocity: > 0 } => 1,
            NoteEvent { Velocity: 0 } => -1,
            ControlChangeEvent { Controller: MidiController.Sustain, ControllerValue: 255 } => 1,
            ControlChangeEvent { Controller: MidiController.Sustain, ControllerValue: 0 } => -1,
            _ => 0
        };
    }

    public bool IsNote(MidiEvent midiEvent)
    {
        return midiEvent is NoteEvent;
    }
}
