using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

public class NAudioMidiFileSaver : IMidiFileSaver<MidiEvent>
{
    public void Save(IEnumerable<MidiEvent> events, string filePath, int timeDivision)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (directory != null && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

        var midiEventCollection = new MidiEventCollection(1, timeDivision);
        var midiEvents = events as MidiEvent[] ?? events.ToArray();
        MidiTrackBuilder builder = new(midiEvents);
        var tracks = builder.BuildTracks();
        foreach (var track in tracks) midiEventCollection.AddTrack(track.ToList());

        MidiFile.Export(filePath, midiEventCollection);
    }
}
