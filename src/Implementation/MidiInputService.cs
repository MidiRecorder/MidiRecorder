using Microsoft.Extensions.Logging;
using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

public class MidiInputService : IMidiInputService
{
    private readonly ILogger<MidiInputService> _logger;

    public MidiInputService(ILogger<MidiInputService> logger)
    {
        _logger = logger;
    }

    public IEnumerable<MidiInput> GetMidiInputs()
    {
        for (var device = 0; device < MidiIn.NumberOfDevices; device++)
        {
            MidiInCapabilities midiInCapabilities = MidiIn.DeviceInfo(device);
            yield return CreateMidiInput(midiInCapabilities);
        }
    }

    public IEnumerable<int> GetMidiInputId(string midiInputName)
    {
        var midiInCapabilities = GetMidiInputs().ToArray();

        if (midiInCapabilities.Length == 0)
        {
            _logger.LogWarning("You have no MIDI inputs");
            yield break;
        }

        if (midiInputName == "*")
        {
            foreach (var i in Enumerable.Range(0, midiInCapabilities.Length))
            {
                _logger.LogInformation("MIDI Input: {Name}", midiInCapabilities[i].Name);
                yield return i;
            }

            yield break;
        }

        int? selectedIdx = null;
        if (int.TryParse(midiInputName, out var s))
        {
            selectedIdx = s >= 0 && s < midiInCapabilities.Length ? s : null;
        }

        selectedIdx ??= midiInCapabilities.Select((port, idx) => new { port, idx }).FirstOrDefault(
            x => string.Equals(x.port.Name, midiInputName, StringComparison.OrdinalIgnoreCase))?.idx;

        if (selectedIdx == null)
        {
            _logger.LogWarning("MIDI input not found");
            yield break;
        }

        _logger.LogInformation("MIDI Input: {Name}", midiInCapabilities[selectedIdx.Value].Name);
        yield return selectedIdx.Value;
    }

    private MidiInput CreateMidiInput(MidiInCapabilities capabilities)
    {
        return new MidiInput(capabilities.ProductName);
    }
}
