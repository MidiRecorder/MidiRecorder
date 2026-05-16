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
        var markerSuffix = string.IsNullOrWhiteSpace(options.Marker) ? "_good" : options.Marker.Trim();
        if (!SavedFileMarker.TryValidateSuffix(markerSuffix, out var markerError))
        {
            return (null, markerError);
        }

        var replayPath = string.IsNullOrWhiteSpace(options.ReplayMidi) ? null : options.ReplayMidi.Trim();
        if (replayPath != null)
        {
            if (!File.Exists(replayPath))
            {
                return (null, $"Replay MIDI file not found: '{replayPath}'");
            }

            return (
                new TypedRecordOptions(
                    TimeSpan.FromMilliseconds(options.DelayToSave),
                    TimeSpan.FromMilliseconds(30000),
                    options.PathFormatString,
                    options.MidiResolution,
                    Array.Empty<int>(),
                    markerSuffix,
                    string.IsNullOrWhiteSpace(options.RawCapturePath) ? null : options.RawCapturePath.Trim(),
                    replayPath,
                    options.ReplayRealtime),
                "OK");
        }

        var inputIds = options.MidiInputs.SelectMany(_service.GetMidiInputId).Distinct().ToArray();
        if (inputIds.Length == 0)
        {
            return (null, $"No MIDI inputs for '{string.Join(", ", options.MidiInputs)}' could be located");
        }

        return (
            new TypedRecordOptions(
                TimeSpan.FromMilliseconds(options.DelayToSave),
                TimeSpan.FromMilliseconds(30000),
                options.PathFormatString,
                options.MidiResolution,
                inputIds,
                markerSuffix,
                string.IsNullOrWhiteSpace(options.RawCapturePath) ? null : options.RawCapturePath.Trim(),
                null,
                false),
            "OK");
    }
}
