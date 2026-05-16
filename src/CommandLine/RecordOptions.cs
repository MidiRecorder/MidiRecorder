using System.Diagnostics.CodeAnalysis;
using CommandLine;
using CommandLine.Text;

namespace MidiRecorder.CommandLine;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[Verb("record", true, HelpText = "Records MIDI to files")]
public class RecordOptions
{
    public RecordOptions(
        IEnumerable<string> midiInputs,
        long delayToSave,
        string pathFormatString,
        int midiResolution,
        string? rawCapturePath,
        string? replayMidi,
        bool replayRealtime,
        string marker)
    {
        MidiInputs = midiInputs;
        PathFormatString = pathFormatString;
        DelayToSave = delayToSave;
        MidiResolution = midiResolution;
        RawCapturePath = rawCapturePath;
        ReplayMidi = replayMidi;
        ReplayRealtime = replayRealtime;
        Marker = marker;
    }

    [Option('i', "input", HelpText = "MIDI Input name or index", Default = new[] { "*" }, Separator = ',')]
    public IEnumerable<string> MidiInputs { get; }

    [Option(
        'd',
        "delay",
        HelpText = "Delay (in milliseconds) before saving the latest recorded MIDI events",
        Default = 5000)]
    public long DelayToSave { get; }

    [Option('f', "format", HelpText = "Format String for output MIDI path", Default = "{Now:yyyyMMddHHmmss}.mid")]
    public string PathFormatString { get; }

    [Option('r', "resolution", HelpText = "MIDI resolution in pulses per quarter note (PPQN)", Default = 480)]
    public int MidiResolution { get; }

    [Option(
        "raw-capture",
        HelpText =
            "If set, writes a single debug .mid on exit with the same event stream the tool receives (one track per input port, timing from the driver timestamps).")]
    public string? RawCapturePath { get; }

    [Option(
        "replay",
        HelpText =
            "Play events from this .mid file instead of hardware input (use with captures from --raw-capture, or any Type 1 file with per-port tracks).")]
    public string? ReplayMidi { get; }

    [Option(
        "replay-realtime",
        HelpText = "When using --replay, pace events using tick timing from the file (default: replay as fast as possible).")]
    public bool ReplayRealtime { get; }

    [Option(
        "marker",
        Default = "_good",
        HelpText = "Suffix appended to the filename when you press m to mark the last saved file (default: _good).")]
    public string Marker { get; }

    [Usage]
    public static IEnumerable<Example> Examples
    {
        get
        {
            yield return new Example(
                "normal scenario",
                new RecordOptions(new[] { "M1", "Triton" }, 5000, "{Now}.mid", 480, null, null, false, "_good"));
            yield return new Example(
                "date-based folder structure",
                new RecordOptions(
                    new[] { "Impulse" },
                    7000,
                    @"{Now:yyyy}\{Now:MM}\{Now:dd}\{Now:yyyyMMddHHmmss}_{NumberOfNoteEvents}.mid",
                    960,
                    null,
                    null,
                    false,
                    "_good"));
        }
    }
}
