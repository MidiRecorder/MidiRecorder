using System;
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
            MidiTrackBuilder builder = new(events);
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

            string? dirName = Path.GetDirectoryName(filePath);
            if (dirName != null)
            {
                Directory.CreateDirectory(dirName);
            }

            MidiFile.Write(file, filePath);
        }
    }
}
