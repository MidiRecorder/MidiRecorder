using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using MidiRecorder;
namespace Tests
{
    [TestClass]
    public class RecordingReceiverTests
    {
        [TestMethod]
        public void MegaFormatTest()
        {
            var actual = StringEx.TranslateToStandardFormatString("{Number} {Date:yyyyMM}-{Number:x}");
            actual.format.Should().Be("{0} {1:yyyyMM}-{0:x}");
            actual.itemNames.Should().BeEquivalentTo("Number", "Date");

            StringEx.Format("{Number} {Date:yyyyMM}-{Number,4:x}", new { Number = 234, Date = new DateTime(2021, 02, 14) })
                .Should().Be("234 202102-  ea");
        }

        public record TestRecord(int Number, DateTime Date);
    }
}
