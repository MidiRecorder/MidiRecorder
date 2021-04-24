using System;
using System.Linq;
using CannedBytes.Midi.IO;
using CannedBytes.Midi.Message;

namespace MidiRecorder.CommandLine
{
    public static class FormatTester
    {

        public static string TestFormat(string pathFormatString)
        {
            var factory = new MidiMessageFactory();

            int ToInteger(byte b1, byte b2, byte b3, byte b4)
            {
                byte[] bytes = { b1, b2, b3, b4 };

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);

                return BitConverter.ToInt32(bytes, 0);
            }

            var eventList = new[]
                {
                    ToInteger(0x00, 0x64, 0x24, 0x90),
                    ToInteger(0x00, 0x00, 0x24, 0x80),
                    ToInteger(0x00, 0x64, 0x2D, 0x90),
                    ToInteger(0x00, 0x00, 0x2D, 0x80),
                    ToInteger(0x00, 0x64, 0x2B, 0x90),
                    ToInteger(0x00, 0x00, 0x2B, 0x80),
                }
                .Select((msg, index) =>
                    new MidiFileEvent
                    {
                        AbsoluteTime = index * 120,
                        DeltaTime = 120,
                        Message = factory.CreateShortMessage((int) msg)
                    });
            var context = new MidiFileContext(eventList, DateTime.Now, Guid.NewGuid());

            return context.BuildFilePath(pathFormatString);
        }

    }
}