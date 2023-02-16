using Microsoft.Extensions.Logging;
using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

public class NAudioMidiFormatTester : IFormatTester
{
    private readonly IMidiEventAnalyzer<MidiEventWithPort> _analyzer;
    private readonly ILogger<NAudioMidiFormatTester> _logger;
    public NAudioMidiFormatTester(IMidiEventAnalyzer<MidiEventWithPort> analyzer, ILogger<NAudioMidiFormatTester> logger)
    {
        _analyzer = analyzer;
        _logger = logger;
    }

    public bool TestFormat(string pathFormatString)
    {
        var eventList = new []
        {
            new MidiEventWithPort(new NoteOnEvent(11, 1, 78, 34, 333), 0)
        };

        var context = new MidiFileContext<MidiEventWithPort>(eventList, DateTime.Now, Guid.NewGuid(), _analyzer);

        try
        {
            _ = context.BuildFilePath(pathFormatString);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Output path format (-f) error: {Message}", ex.Message);
            return false;
        }
    }

}
