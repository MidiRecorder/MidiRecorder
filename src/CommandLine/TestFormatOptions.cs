using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CommandLine;
using CommandLine.Text;

namespace MidiRecorder.CommandLine
{
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [Verb("testformat", HelpText = "Tests an output path format string")]
    public class TestFormatOptions
    {
        public TestFormatOptions(string pathFormatString)
        {
            PathFormatString = pathFormatString;
        }

        [Option('f', "format", HelpText = "Format String for output MIDI path", Required = true)]
        public string PathFormatString { get; }

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("normal scenario", new TestFormatOptions("{Now}.mid"));
                yield return new Example("date-based folder structure", new TestFormatOptions(@"{Now:yyyy}\{Now:MM}\{Now:dd}\{Now:yyyyMMddHHmmss}_{NumberOfNoteEvents}.mid"));
            }
        }
    }
}