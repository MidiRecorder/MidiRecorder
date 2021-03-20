using System.Collections.Generic;
using System.IO;
using System.Linq;
using CannedBytes.Midi.IO;

namespace MidiRecorder.CommandLine
{
    internal static class MidiFileSerializer
    {
        public static void Serialize(IEnumerable<MidiFileEvent> events, string filePath, int timeDivision)
        {
            MidiTrackBuilder builder = new(events);
            var tracks = builder.BuildTracks();

            var file = new MidiFile
            {
                Header = new MThdChunk
                {
                    Format = (ushort)MidiFileFormat.MultipleTracks,
                    NumberOfTracks = (ushort)tracks.Count(),
                    TimeDivision = (ushort)timeDivision
                },
                Tracks = tracks.Select(x => new MTrkChunk { Events = x })
            };

            string? dirName = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            MidiFile.Write(file, filePath);
        }
    }
}
