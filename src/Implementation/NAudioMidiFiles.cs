using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

internal static class NAudioMidiFiles
{
    public static void Save(IEnumerable<IEnumerable<NAudioMidiEvent>> tracks, string filePath, int timeDivision)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var midiEventCollection = new MidiEventCollection(1, timeDivision);
        foreach (var track in tracks)
        {
            midiEventCollection.AddTrack(track.Select(mp => mp.MidiEvent).ToList());
        }

        MidiFile.Export(filePath, midiEventCollection);
    }
    
    public static IEnumerable<IEnumerable<NAudioMidiEvent>> Open(string filePath, int port)
    {
        return new MidiFile(filePath).Events.Select(t => t.Select(e => new NAudioMidiEvent(e, port)));
    }
}
