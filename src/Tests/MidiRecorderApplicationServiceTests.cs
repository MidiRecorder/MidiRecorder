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
public class MidiRecorderApplicationServiceTests
{
    [TestMethod]
    public void GroupsToSave_SingleGroupTest()
    {
        var evts = new[]
        {
            Recorded.OnNext(100, "1 C5"),
            Recorded.OnNext(105, "-1 C5"),
            Recorded.OnNext(116, "1 C7"),
            Recorded.OnNext(120, "-1 C7")
        };

        var timeoutToSave = TimeSpan.FromTicks(20);
        var delayToSave = TimeSpan.FromTicks(30);
        var scheduler = new TestScheduler();
        var sut = BuildSut(scheduler, evts, timeoutToSave, delayToSave);

        var result = sut.SplitGroups;

        var result2 = PrepareResult(result, scheduler);
        result2.Should().BeEquivalentTo(new[]
        {
            new []
            {
                Recorded.Create(101, "1 C5"),
                Recorded.Create(106, "-1 C5"),
                Recorded.Create(117, "1 C7"),
                Recorded.Create(121, "-1 C7")               
            }
        });
    }

    [TestMethod]
    public void GroupsToSave_SplitByRelease()
    {
        var evts = new[]
        {
            Recorded.OnNext(100, "1 C5"),
            Recorded.OnNext(105, "-1 C5"),
            Recorded.OnNext(200, "1 C7"),
            Recorded.OnNext(205, "-1 C7")
        };

        TimeSpan timeoutToSave = TimeSpan.FromTicks(20);
        TimeSpan delayToSave = TimeSpan.FromTicks(30);

        var scheduler = new TestScheduler();
        var sut = BuildSut(scheduler, evts, timeoutToSave, delayToSave);

        var result = sut.SplitGroups;

        var result2 = PrepareResult(result, scheduler);
        result2.Should().BeEquivalentTo(new[]
        {
            new []
            {
                Recorded.Create(101, "1 C5"),
                Recorded.Create(106, "-1 C5"),
            },
            new []
            {
                Recorded.Create(201, "1 C7"),
                Recorded.Create(206, "-1 C7")               
            }
        });
    }

    [TestMethod]
    public void GroupsToSave_SplitByHeldNote()
    {
        var evts = new[]
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
        var sut = BuildSut(scheduler, evts, timeoutToSave, delayToSave);

        var result = sut.SplitGroups;

        var result2 = PrepareResult(result, scheduler);
        result2.Should().BeEquivalentTo(new[]
        {
            new []
            {
                Recorded.Create(101, " 1 C5"),
                Recorded.Create(106, "-1 C5"),
                Recorded.Create(111, " 1 C6 held"),
            },
            new []
            {
                Recorded.Create(193, "-1 C6 held"),
                Recorded.Create(201, " 1 C7"),
                Recorded.Create(206, "-1 C7")               
            }
        });
    }

    [TestMethod]
    public void GroupsToSave_SplitByHeldNote2()
    {
        var evts = new[]
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
        var sut = BuildSut(scheduler, evts, timeoutToSave, delayToSave);

        var result = sut.SplitGroups;

        var result2 = PrepareResult(result, scheduler);
        result2.Should().BeEquivalentTo(new[]
        {
            new []
            {
                Recorded.Create(101, " 1 C5"),
                Recorded.Create(106, "-1 C5"),
                Recorded.Create(111, " 1 C6 held"),
            },
            new []
            {
                Recorded.Create(193, "-1 C6 held"),
                Recorded.Create(201, " 1 C7"),
                Recorded.Create(206, "-1 C7")               
            }
        });
    }

    private static Recorded<string>[][] PrepareResult(IObservable<IObservable<string>> result, TestScheduler scheduler)
    {
        var result2 = result.SelectMany((observable, index) => observable.Select(x => (index, x)))
            .WaitAndGetRecorded(scheduler).GroupBy(x => x.Value.index, x => Recorded.Create(x.Time, x.Value.x))
            .Select(x => x.ToArray()).ToArray();
        return result2;
    }

    private static MidiSplit<string> BuildSut(TestScheduler scheduler, Recorded<Notification<string>>[] evts, TimeSpan timeoutToSave, TimeSpan delayToSave)
    {
        int NoteAndSustainPedalCount(string s) => int.Parse(s.Trim().Split(' ')[0]);
        var allEvents = scheduler.CreateColdObservable(evts);
        var sut = new MidiSplitter<string>(scheduler).Build(allEvents, NoteAndSustainPedalCount, timeoutToSave, delayToSave);
        return sut;
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
