namespace MidiRecorder.Application;

public record MidiFileInfo<TMidiEvent>(IEnumerable<IEnumerable<TMidiEvent>> Tracks, string FilePath);