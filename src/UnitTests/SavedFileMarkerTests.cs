using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MidiRecorder.Application;

namespace MidiRecorder.Tests;

[TestClass]
public class SavedFileMarkerTests
{
    [TestMethod]
    public void ApplySuffix_PlainFileName_AppendsBeforeExtension()
    {
        SavedFileMarker.ApplySuffix("20260516143022.mid", "_good")
            .Should()
            .Be("20260516143022_good.mid");
    }

    [TestMethod]
    public void ApplySuffix_NestedPath_PreservesDirectory()
    {
        SavedFileMarker.ApplySuffix(@"recordings\2026\foo.mid", "_good")
            .Should()
            .Be(Path.Combine("recordings", "2026", "foo_good.mid"));
    }

    [TestMethod]
    public void ApplySuffix_DoubleExtension_AppendsBeforeLastExtension()
    {
        SavedFileMarker.ApplySuffix("archive.tar.mid", "_good")
            .Should()
            .Be("archive.tar_good.mid");
    }

    [TestMethod]
    public void ApplySuffix_NoDirectory_UsesFileNameOnly()
    {
        SavedFileMarker.ApplySuffix("foo.mid", "_keeper")
            .Should()
            .Be("foo_keeper.mid");
    }

    [TestMethod]
    public void TryValidateSuffix_Empty_ReturnsFalse()
    {
        SavedFileMarker.TryValidateSuffix("", out var error).Should().BeFalse();
        error.Should().NotBeEmpty();
    }

    [TestMethod]
    public void TryValidateSuffix_InvalidCharacter_ReturnsFalse()
    {
        SavedFileMarker.TryValidateSuffix("bad|suffix", out var error).Should().BeFalse();
        error.Should().Contain("invalid filename");
    }

    [TestMethod]
    public void TryValidateSuffix_Valid_ReturnsTrue()
    {
        SavedFileMarker.TryValidateSuffix("_good", out var error).Should().BeTrue();
        error.Should().BeEmpty();
    }

}
