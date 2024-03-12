using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MidiRecorder.Application;
using MidiRecorder.Application.Implementation;
using NAudio.Midi;

namespace MidiRecorder.Tests;

[TestClass]
public class FormatDataTests
{
    [TestMethod]
    public void Ctor()
    {
        var eventList = new[]
        {
            new MidiEventWithPort(new NoteOnEvent(11, 1, 78, 34, 333), 3),
            new MidiEventWithPort(new TempoEvent(333, 1093), 3)
        };
        MidiFileContext.BuildFilePath("{NumberOfNoteEvents}", eventList, DateTime.Now, Guid.NewGuid(), NAudioMidiEventAnalyzer.IsNote);
        var x = new FormatData<MidiEventWithPort>(
            DateTime.Now,
            eventList,
            Guid.NewGuid(),
            NAudioMidiEventAnalyzer.IsNote);

        x.NumberOfEvents.Should().Be(2);
        x.NumberOfNoteEvents.Should().Be(1);
    }    
}
