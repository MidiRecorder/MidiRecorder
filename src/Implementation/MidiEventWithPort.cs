using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

public record MidiEventWithPort(MidiEvent MidiEvent, int Port)
{
    public override string ToString()
    {
        return $"P{Port} {MidiEvent}";
    }
}
