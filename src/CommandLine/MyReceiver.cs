using System;
using System.IO.Abstractions;
using CannedBytes.Midi;

namespace MidiRecorder
{
    public class MyReceiver : IMidiDataReceiver
    {
        private readonly IFileSystem _fileSystem;

        public MyReceiver(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public void ShortData(int data, long timestamp)
        {
            Console.WriteLine($"{data} {timestamp}");
        }

        public void LongData(MidiBufferStream buffer, long timestamp)
        {
            // not used for short midi messages
        }
    }
}
