using MidiRecorder.Application;

namespace MidiRecorder.CommandLine;

internal class OptionsValidator : IOptionsValidator
{
    private readonly IMidiInputService _service;

    public OptionsValidator(IMidiInputService service)
    {
        _service = service;
    }

    public (TypedRecordOptions? typedRecordOptions, string errorMessage) Validate(RecordOptions options)
    {
        int[] inputIds = options.MidiInputs.SelectMany(_service.GetMidiInputId).Distinct().ToArray();
        if (inputIds.Length == 0)
        {
            return (null, $"No MIDI inputs for '{string.Join(", ", options.MidiInputs)}' could be located");
        }
        
        return (new TypedRecordOptions(
            TimeSpan.FromMilliseconds(options.DelayToSave),
            TimeSpan.FromMilliseconds(30000),
            options.PathFormatString,
            options.MidiResolution,
            inputIds), "OK");
    }
}
