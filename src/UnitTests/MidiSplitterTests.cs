using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MidiRecorder.Application;

namespace MidiRecorder.Tests;

[TestClass]
public class MidiSplitterTests
{
    [TestMethod]
    public void Split_SingleGroupTest()
    {
        var events = new[]
        {
            Recorded.OnNext(100, "1 C5"),
            Recorded.OnNext(105, "-1 C5"),
            Recorded.OnNext(116, "1 C7"),
            Recorded.OnNext(120, "-1 C7")
        };

        TimeSpan timeoutToSave = TimeSpan.FromTicks(20);
        TimeSpan delayToSave = TimeSpan.FromTicks(30);
        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeoutToSave, delayToSave);

        var result = sut.SplitGroups;

        var result2 = PrepareResult(result, scheduler);
        result2.Should().BeEquivalentTo(
            new[]
            {
                new[]
                {
                    Recorded.Create(101, "1 C5"),
                    Recorded.Create(106, "-1 C5"),
                    Recorded.Create(117, "1 C7"),
                    Recorded.Create(121, "-1 C7")
                }
            });
    }

    [TestMethod]
    public void Split_SplitByRelease()
    {
        var events = new[]
        {
            Recorded.OnNext(100, "1 C5"),
            Recorded.OnNext(105, "-1 C5"),
            Recorded.OnNext(200, "1 C7"),
            Recorded.OnNext(205, "-1 C7")
        };

        TimeSpan timeoutToSave = TimeSpan.FromTicks(20);
        TimeSpan delayToSave = TimeSpan.FromTicks(30);

        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeoutToSave, delayToSave);

        var result = sut.SplitGroups;

        var result2 = PrepareResult(result, scheduler);
        result2.Should().BeEquivalentTo(
            new[]
            {
                new[] { Recorded.Create(101, "1 C5"), Recorded.Create(106, "-1 C5") },
                new[] { Recorded.Create(201, "1 C7"), Recorded.Create(206, "-1 C7") }
            });
    }

    [TestMethod]
    public void Split_SplitByHeldNote()
    {
        var events = new[]
        {
            Recorded.OnNext(100, " 1 C5"),
            Recorded.OnNext(105, "-1 C5"),
            Recorded.OnNext(110, " 1 C6 held"),
            Recorded.OnNext(192, "-1 C6 held"),
            Recorded.OnNext(200, " 1 C7"),
            Recorded.OnNext(205, "-1 C7")
        };

        TimeSpan timeoutToSave = TimeSpan.FromTicks(20);
        TimeSpan delayToSave = TimeSpan.FromTicks(30);

        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeoutToSave, delayToSave);

        var result = sut.SplitGroups;

        var result2 = PrepareResult(result, scheduler);
        result2.Should().BeEquivalentTo(
            new[]
            {
                new[]
                {
                    Recorded.Create(101, " 1 C5"),
                    Recorded.Create(106, "-1 C5"),
                    Recorded.Create(111, " 1 C6 held")
                },
                new[]
                {
                    Recorded.Create(193, "-1 C6 held"),
                    Recorded.Create(201, " 1 C7"),
                    Recorded.Create(206, "-1 C7")
                }
            });
    }

    [TestMethod]
    public void Split_OtherEventsAreIgnored()
    {
        var events = new[]
        {
            Recorded.OnNext(100, " 1 C5"),
            Recorded.OnNext(103, " 0 event"),
            Recorded.OnNext(105, "-1 C5"),
            Recorded.OnNext(110, " 0 event"),
            Recorded.OnNext(120, " 0 event"),
            Recorded.OnNext(130, " 0 event"),
            Recorded.OnNext(140, " 0 event"),
            Recorded.OnNext(150, " 0 event"),
            Recorded.OnNext(160, " 0 event"),
            Recorded.OnNext(200, " 1 C7"),
            Recorded.OnNext(202, " 0 event"),
            Recorded.OnNext(205, "-1 C7")
        };

        TimeSpan timeoutToSave = TimeSpan.FromTicks(20);
        TimeSpan delayToSave = TimeSpan.FromTicks(30);

        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeoutToSave, delayToSave);

        var result = sut.SplitGroups;

        var result2 = PrepareResult(result, scheduler);
        result2.Should().BeEquivalentTo(
            new[]
            {
                new[]
                {
                    Recorded.Create(101, " 1 C5"),
                    Recorded.Create(104, " 0 event"),
                    Recorded.Create(106, "-1 C5"),
                    Recorded.Create(111, " 0 event"),
                    Recorded.Create(121, " 0 event"),
                    Recorded.Create(131, " 0 event")
                },
                new[]
                {
                    Recorded.Create(141, " 0 event"),
                    Recorded.Create(151, " 0 event"),
                    Recorded.Create(161, " 0 event"),
                    Recorded.Create(201, " 1 C7"),
                    Recorded.Create(203, " 0 event"),
                    Recorded.Create(206, "-1 C7")
                }
            });
    }

    [TestMethod]
    public void GroupsToSave_SplitByHeldNote2()
    {
        var events = new[]
        {
            Recorded.OnNext(100, " 1 C5"),
            Recorded.OnNext(105, "-1 C5"),
            Recorded.OnNext(110, " 1 C6 held"),
            Recorded.OnNext(192, "-1 C6 held"),
            Recorded.OnNext(200, " 1 C7"),
            Recorded.OnNext(205, "-1 C7")
        };

        TimeSpan timeoutToSave = TimeSpan.FromTicks(20);
        TimeSpan delayToSave = TimeSpan.FromTicks(30);

        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeoutToSave, delayToSave);

        var result = sut.SplitGroups;

        var result2 = PrepareResult(result, scheduler);
        result2.Should().BeEquivalentTo(
            new[]
            {
                new[]
                {
                    Recorded.Create(101, " 1 C5"),
                    Recorded.Create(106, "-1 C5"),
                    Recorded.Create(111, " 1 C6 held")
                },
                new[]
                {
                    Recorded.Create(193, "-1 C6 held"),
                    Recorded.Create(201, " 1 C7"),
                    Recorded.Create(206, "-1 C7")
                }
            });
    }

    private static MidiSplit<string> CreateSplit(TestScheduler scheduler, Recorded<Notification<string>>[] events,
        TimeSpan timeoutToSave, TimeSpan delayToSave)
    {
        var allEvents = scheduler.CreateColdObservable(events);
        var sut = new MidiSplitter<string>(scheduler);
        var split = sut.Split(allEvents, NoteAndSustainPedalCount, timeoutToSave, delayToSave);
        return split;
    }

    private static Recorded<string>[][] PrepareResult(IObservable<IObservable<string>> result, TestScheduler scheduler)
    {
        return result.SelectMany((observable, index) => observable.Select(x => (index, x)))
            .WaitAndGetRecorded(scheduler).GroupBy(x => x.Value.index, x => Recorded.Create(x.Time, x.Value.x))
            .Select(x => x.ToArray()).ToArray();
    }

    private static int NoteAndSustainPedalCount(string s)
    {
        return int.Parse(s.Trim().Split(' ')[0]);
    }
}

public static class Recorded
{
    public static Recorded<T> Create<T>(long time, T value)
    {
        return new Recorded<T>(time, value);
    }

    public static Recorded<Notification<T>> OnNext<T>(long time, T value)
    {
        return Create(time, Notification.CreateOnNext(value));
    }
}
