using Microsoft.Extensions.Logging;

namespace MidiRecorder.Application.ListMidiInputs;

public class ListMidiInputsService<TMidiEvent> where TMidiEvent : IMidiEvent
{
    private readonly ILogger<ListMidiInputsService<TMidiEvent>> _logger;
    private readonly MidiTools<TMidiEvent> _midiTools;

    public ListMidiInputsService(
        ILogger<ListMidiInputsService<TMidiEvent>> logger,
        MidiTools<TMidiEvent> midiToolsService)
    {
        _logger = logger;
        _midiTools = midiToolsService;
    }

    public int ListMidiInputs() =>
        _midiTools.GetMidiInputs().ToSeq()
            .Match(
                () =>
                {
                    _logger.LogError("{Message}", "No MIDI inputs");
                    return 1;
                },
                midiInCapabilities =>
                {
                    _ = midiInCapabilities.Iter((i, midiInput) => Console.WriteLine($"{i}. {midiInput.Name}"));
                    return 0;
                });
}