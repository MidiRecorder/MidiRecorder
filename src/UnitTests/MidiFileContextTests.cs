using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MidiRecorder.Application;
using MidiRecorder.Application.Implementation;
using NAudio.Midi;

namespace MidiRecorder.Tests;

[TestClass]
public class MidiFileContextTests
{
    [TestMethod]
    public void BuildFilePath_AllData()
    {
        var analyzer = new NAudioMidiEventAnalyzer();
        var eventList = new[]
        {
            new MidiEventWithPort(new NoteOnEvent(0, 1, 60, 100, 0), 0),
            new MidiEventWithPort(new ControlChangeEvent(0, 1, MidiController.Sustain, 127), 0)
        };

        var guid = new Guid("64ea2c65-12b9-44c7-8d0b-fcf9a298f156");
        var context = new MidiFileContext<MidiEventWithPort>(
            eventList,
            new DateTime(2024, 3, 17, 14, 34, 22),
            guid,
            analyzer);
        var result = context.BuildFilePath(
            "{Guid}/{NumberOfNoteEvents}/{NumberOfEvents}/{Now:yyyy}/{Now:MM}/{Now:dd_HH_mm_ss}.mid");
        result.Should().Be("64ea2c65-12b9-44c7-8d0b-fcf9a298f156/1/2/2024/03/17_14_34_22.mid");
    }
}
