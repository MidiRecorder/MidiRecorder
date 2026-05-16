using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MidiRecorder.Application.Implementation;
using NAudio.Midi;

namespace MidiRecorder.Tests;

[TestClass]
public class NAudioMidiEventAnalyzerTests
{
    private readonly NAudioMidiEventAnalyzer _analyzer = new();

    [TestMethod(DisplayName = "Regression for #17")]
    public void NoteAndSustainPedalCount_NoteOffCommand_DecrementsHeldCount()
    {
        var noteOff = new MidiEventWithPort(
            new NoteEvent(0, 1, MidiCommandCode.NoteOff, 60, 0),
            0);
        _analyzer.NoteAndSustainPedalCount(noteOff).Should().Be(-1);
    }

    [TestMethod(DisplayName = "Regression for #17")]
    public void IsNote_WrappedNoteEvent_ReturnsTrue()
    {
        var noteOn = new MidiEventWithPort(new NoteOnEvent(0, 1, 60, 100, 0), 0);
        _analyzer.IsNote(noteOn).Should().BeTrue();
    }
}
