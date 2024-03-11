using LanguageExt;
using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

public static class NAudioMidiFormatTester
{
    public static Validation<string, Unit> TestFormat(
        string pathFormatString,
        Func<MidiEventWithPort, bool> isNote)
    {
        var eventList = new[] { new MidiEventWithPort(new NoteOnEvent(11, 1, 78, 34, 333), 0) };
        
        try
        {
            _ = MidiFileContext.BuildFilePath(pathFormatString, eventList, DateTime.Now, Guid.NewGuid(), isNote);
            return Prelude.unit;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
