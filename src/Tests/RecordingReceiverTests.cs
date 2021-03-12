using System.IO.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MidiRecorder;
using Moq;

namespace Tests
{
    [TestClass]
    public class RecordingReceiverTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            var x = new RecordingReceiver(fileSystemMock.Object);
        }
    }
}
