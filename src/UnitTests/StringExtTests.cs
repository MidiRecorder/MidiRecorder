using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MidiRecorder.Application;

namespace MidiRecorder.Tests;

public static class TestSchedulerExtensions
{
    public static Recorded<T>[] WaitAndGetRecorded<T>(this IObservable<T> o, TestScheduler scheduler)
    {
        var testObserver = scheduler.Start(() => o, 0, 0, 5000);

        return testObserver.Messages.Select(r => new Recorded<T>(r.Time, r.Value.Value)).ToArray();
    }
}

[TestClass]
public class StringExtTests
{
    [TestMethod]
    public void TranslateToStandardFormatString_HappyPath()
    {
        var actual = StringExt.TranslateToStandardFormatString("{Number} {Date:yyyyMM}-{Number:x}");
        actual.format.Should().Be("{0} {1:yyyyMM}-{0:x}");
        actual.itemNames.Should().BeEquivalentTo("Number", "Date");
    }

    [TestMethod]
    public void Format_EmptyBraces_ThrowsFormatException()
    {
        Action action = () => StringExt.Format(
            "{Number} {Date:yyyyMM}-{}",
            new { Number = 234, Date = new DateTime(2021, 02, 14) });

        action.Should().Throw<FormatException>();
    }

    [TestMethod]
    public void Format_NoItemNameAndThenColon_ThrowsFormatException()
    {
        Action action = () => StringExt.Format(
            "{Number} {:yyyyMM}-{}",
            new { Number = 234, Date = new DateTime(2021, 02, 14) });

        action.Should().Throw<FormatException>();
    }

    [TestMethod]
    public void Format_NoItemNameAndThenComma_ThrowsFormatException()
    {
        Action action = () => StringExt.Format(
            "{Number} {,7}-{}",
            new { Number = 234, Date = new DateTime(2021, 02, 14) });

        action.Should().Throw<FormatException>();
    }

    [TestMethod]
    public void Format_NullFormatString_ThrowsArgumentException()
    {
        Action action = () => StringExt.Format(null, new { Number = 234, Date = new DateTime(2021, 02, 14) });

        action.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Format_NullObject_ThrowsArgumentNullException()
    {
        Action action = () => StringExt.Format("{Number}", null);

        action.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Format_NullObjectIfNotNeeded_Success()
    {
        Action action = () => StringExt.Format("No placeholders", null);

        action.Should().NotThrow();
    }

    [TestMethod]
    public void Format_ValueTupleData_ThrowsArgumentException()
    {
        Action action = () => StringExt.Format("{Number}", (Number: 7, Text: "hello"));

        action.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Format_PropertyNotFound_ThrowsArgumentException()
    {
        Action action = () => StringExt.Format("{Other}", new { Number = 7, Text = "hello" });

        action.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Format_HappyPath()
    {
        StringExt.Format("{Number} {Date:yyyyMM}-{Number,4:x}", new { Number = 234, Date = new DateTime(2021, 02, 14) })
            .Should()
            .Be("234 202102-  ea");
    }

    public record TestRecord(int Number, DateTime Date);
}

public class TestLogger<T> : ILogger<T>, IDisposable
{
    private readonly List<string> _traces = new();

    public IEnumerable<string> Traces => _traces;

    public void Dispose()
    {
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        _traces.Add(state.ToString());
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return this;
    }
}
