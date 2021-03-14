using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using CannedBytes.Midi.IO;

namespace MidiRecorder
{
    internal static class MidiFileSerializer
    {
        public static void Serialize(IEnumerable<MidiFileEvent> events, string filePath)
        {
            MidiTrackBuilder builder = new MidiTrackBuilder(events);
            var tracks = builder.BuildTracks();

            var file = new MidiFile
            {
                Header = new MThdChunk
                {
                    Format = (ushort)MidiFileFormat.MultipleTracks,
                    NumberOfTracks = (ushort)tracks.Count(),
                    TimeDivision = 960
                },
                Tracks = tracks.Select(x => new MTrkChunk { Events = x })
            };
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            MidiFile.Write(file, filePath);
        }
    }
}
