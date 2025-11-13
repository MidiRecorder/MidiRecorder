using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MidiRecorder.Application.Implementation;

namespace MidiRecorder.Tests;

[TestClass]
public class TrackBuilderTests
{
    [TestMethod]
    [Description("Regression for #17")]
    public void Test()
    {
        NAudioMidiTrackBuilder.BuildTracks([]).Should().BeEmpty();
    }    
}
