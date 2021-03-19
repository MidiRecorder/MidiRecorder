using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CommandLine;
using CommandLine.Text;

namespace MidiRecorder.CommandLine
{
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [Verb("record", isDefault:true, HelpText = "Records MIDI to files")]
    public class RecordOptions
    {
        public RecordOptions(string midiInput, long delayToSave, string pathFormatString)
        {
            MidiInput = midiInput;
            PathFormatString = pathFormatString;
            DelayToSave = delayToSave;
        }

        [Value(0, MetaName = "MIDI Input", HelpText = "MIDI Input name or index", Required = true)]
        public string MidiInput { get; }

        [Value(1, HelpText = "Delay before saving the latest recorded MIDI events", Required = true)]
        public long DelayToSave { get; }

        [Value(2, HelpText = "Format String for output MIDI path", Required = true)]
        public string PathFormatString { get; }

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("normal scenario", new RecordOptions("Korg M1", 5000, "{Now}.mid"));
                yield return new Example("date-based folder structure", new RecordOptions("Impulse", 7000, @"{Now:yyyy}\{Now:MM}\{Now:dd}\{Now:yyyyMMddHHmmss}_{NumberOfNoteEvents}.mid"));
            }
        }
    }
}