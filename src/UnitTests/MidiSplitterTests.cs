using System;
using System.Linq;
using System.Reactive;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MidiRecorder.Application;
using MidiRecorder.Application.Implementation;
using MidiRecorder.CommandLine;
using NAudio.Midi;

namespace MidiRecorder.Tests;

[TestClass]
public class NoteDurationTests
{

    [TestMethod]
    public void CalculateDurationsTest()
    {
        var events = new[]
        {
            Recorded.OnNext(100, "CC1"),
            Recorded.OnNext(103, "On 45"),
            Recorded.OnNext(105, "On 48"),
            Recorded.OnNext(110, "Off 45"),
            Recorded.OnNext(120, "CC2"),
            Recorded.OnNext(130, "Off 48"),
            Recorded.OnNext(140, "CC3"),
        };
        var scheduler = new TestScheduler();
        var allEvents = scheduler.CreateColdObservable(events);
        var result = NoteDuration.CalculateDurations(
            allEvents,
            x => x.StartsWith("On"),
            x => x.StartsWith("Off"),
            x => int.Parse(x.Split(' ')[1]),
            scheduler);
        var result2 = result.WaitAndGetRecorded(scheduler);
        
        result2.Should()
            .BeEquivalentTo(
                new []
                {
                    Recorded.Create(101, ("CC1", TimeSpan.Zero)),
                    Recorded.Create(104, ("On 45", TimeSpan.Zero)),
                    Recorded.Create(106, ("On 48", TimeSpan.Zero)),
                    Recorded.Create(111, ("Off 45", TimeSpan.FromTicks(111-104))),
                    Recorded.Create(121, ("CC2", TimeSpan.Zero)),
                    Recorded.Create(131, ("Off 48", TimeSpan.FromTicks(131L-106L))),
                    Recorded.Create(141, ("CC3", TimeSpan.Zero)),
                });
    }
}
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

        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(20);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(30);
        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);

        var result2 = Record(sut, scheduler).SplitGroups;
        result2.Should()
            .BeEquivalentTo(
                new[]
                {
                    [
                        Recorded.Create(100, "1 C5"),
                        Recorded.Create(105, "-1 C5"),
                        Recorded.Create(116, "1 C7"),
                        Recorded.Create(120, "-1 C7")
                    ],
                    Array.Empty<Recorded<string>>()
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

        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(20);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(30);

        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);

        var result2 = Record(sut, scheduler).SplitGroups;
        result2.Should()
            .BeEquivalentTo(
                new[]
                {
                    [Recorded.Create(100, "1 C5"), Recorded.Create(105, "-1 C5")],
                    [
                        Recorded.Create(200, "1 C7"),
                        Recorded.Create(205, "-1 C7")
                    ],
                    Array.Empty<Recorded<string>>()
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

        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(20);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(30);

        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);

        var result2 = Record(sut, scheduler).SplitGroups;
        result2.Should()
            .BeEquivalentTo(
                new[]
                {
                    [
                        Recorded.Create(100, " 1 C5"),
                        Recorded.Create(105, "-1 C5"),
                        Recorded.Create(110, " 1 C6 held")
                    ],
                    Array.Empty<Recorded<string>>(),
                    [
                        Recorded.Create(192, "-1 C6 held"),
                        Recorded.Create(200, " 1 C7"),
                        Recorded.Create(205, "-1 C7")
                    ],
                    Array.Empty<Recorded<string>>()
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

        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(20);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(30);

        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);

        var result2 = Record(sut, scheduler).SplitGroups;
        result2.Should()
            .BeEquivalentTo(
                new[]
                {
                    [
                        Recorded.Create(100, " 1 C5"),
                        Recorded.Create(103, " 0 event"),
                        Recorded.Create(105, "-1 C5"),
                        Recorded.Create(110, " 0 event"),
                        Recorded.Create(120, " 0 event"),
                        Recorded.Create(130, " 0 event")
                    ],
                    [
                        Recorded.Create(140, " 0 event"),
                        Recorded.Create(150, " 0 event"),
                        Recorded.Create(160, " 0 event"),
                        Recorded.Create(200, " 1 C7"),
                        Recorded.Create(202, " 0 event"),
                        Recorded.Create(205, "-1 C7")
                    ],
                    Array.Empty<Recorded<string>>()
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

        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(20);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(30);

        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);

        var result2 = Record(sut, scheduler).SplitGroups;
        result2.Should()
            .BeEquivalentTo(
                new[]
                {
                    [
                        Recorded.Create(100, " 1 C5"),
                        Recorded.Create(105, "-1 C5"),
                        Recorded.Create(110, " 1 C6 held")
                    ],
                    Array.Empty<Recorded<string>>(),
                    [
                        Recorded.Create(192, "-1 C6 held"),
                        Recorded.Create(200, " 1 C7"),
                        Recorded.Create(205, "-1 C7")
                    ],
                    Array.Empty<Recorded<string>>()
                });
    }

    [TestMethod]
    public void RegressionTest17()
    {
        var events = new[]
        {
            Recorded.OnNext(100, new MidiEventWithPort(new NoteOnEvent(100, 2, 96, 64, 444), 3)),
            Recorded.OnNext(101, new MidiEventWithPort(new NoteOnEvent(100, 1, 96, 64, 555), 3)),
            Recorded.OnNext(105, new MidiEventWithPort(new NoteEvent(100, 2, MidiCommandCode.NoteOff, 96, 64), 3)),
            Recorded.OnNext(106, new MidiEventWithPort(new NoteEvent(100, 1, MidiCommandCode.NoteOff, 96, 64), 3)),
            Recorded.OnNext(110, new MidiEventWithPort(new NoteOnEvent(100, 2, 95, 64, 666), 3)),
            Recorded.OnNext(111, new MidiEventWithPort(new NoteOnEvent(100, 1, 95, 64, 777), 3)),
            Recorded.OnNext(112, new MidiEventWithPort(new NoteEvent(100, 2, MidiCommandCode.NoteOff, 95, 64), 3)),
            Recorded.OnNext(113, new MidiEventWithPort(new NoteEvent(100, 1, MidiCommandCode.NoteOff, 95, 64), 3)),
        };

        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(20);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(30);

        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff, NAudioMidiEventAnalyzer.NoteAndSustainPedalCount);

        var splitGroups = Record(sut, scheduler).SplitGroups;

        splitGroups.Should()
            .BeEquivalentTo(
                new[]
                {
                    new[]
                    {
                        Recorded.Create(100, "P3 100 NoteOn Ch: 2 C8 Vel:64 Len: 444"),
                        Recorded.Create(101, "P3 100 NoteOn Ch: 1 C8 Vel:64 Len: 555"),
                        Recorded.Create(105, "P3 100 NoteOff Ch: 2 C8 Vel:64"),
                        Recorded.Create(106, "P3 100 NoteOff Ch: 1 C8 Vel:64"),
                        Recorded.Create(110, "P3 100 NoteOn Ch: 2 B7 Vel:64 Len: 666"),
                        Recorded.Create(111, "P3 100 NoteOn Ch: 1 B7 Vel:64 Len: 777"),
                        Recorded.Create(112, "P3 100 NoteOff Ch: 2 B7 Vel:64"),
                        Recorded.Create(113, "P3 100 NoteOff Ch: 1 B7 Vel:64")
                    }
                });
    }

    private RecordedSplit<T> Record<T>(MidiSplit<T> split, TestScheduler scheduler) => new (split, scheduler);
    [TestMethod]
    public void RegressionTest17SavingPoints()
    {
        var events = new[]
        {
            Recorded.OnNext(100, new MidiEventWithPort(new NoteOnEvent(100, 2, 96, 64, 444), 3)),
            Recorded.OnNext(101, new MidiEventWithPort(new NoteOnEvent(100, 1, 96, 64, 555), 3)),
            Recorded.OnNext(105, new MidiEventWithPort(new NoteEvent(100, 2, MidiCommandCode.NoteOff, 96, 64), 3)),
            Recorded.OnNext(106, new MidiEventWithPort(new NoteEvent(100, 1, MidiCommandCode.NoteOff, 96, 64), 3)),
            Recorded.OnNext(110, new MidiEventWithPort(new NoteOnEvent(100, 2, 95, 64, 666), 3)),
            Recorded.OnNext(111, new MidiEventWithPort(new NoteOnEvent(100, 1, 95, 64, 777), 3)),
            Recorded.OnNext(112, new MidiEventWithPort(new NoteEvent(100, 2, MidiCommandCode.NoteOff, 95, 64), 3)),
            Recorded.OnNext(113, new MidiEventWithPort(new NoteEvent(100, 1, MidiCommandCode.NoteOff, 95, 64), 3)),
        };

        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(20);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(30);

        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff, NAudioMidiEventAnalyzer.NoteAndSustainPedalCount);

        var result = sut.SavingPoints;

        var result2 = PrepareResultBottom(result, scheduler);
        result2.Should()
            .BeEquivalentTo(
                new[]
                {
                    Recorded.Create(134, "()"),
                    Recorded.Create(164, "()")
                });
    }

    [TestMethod]
    public void RegressionTest()
    {
        var events = new[]
        {
            Recorded.OnNext(100, new MidiEventWithPort(new NoteOnEvent(100, 2, 96, 64, 444), 3)),
            Recorded.OnNext(101, new MidiEventWithPort(new NoteOnEvent(100, 1, 96, 64, 555), 3)),
            Recorded.OnNext(105, new MidiEventWithPort(new NoteEvent(100, 2, MidiCommandCode.NoteOff, 96, 64), 3)),
            Recorded.OnNext(106, new MidiEventWithPort(new NoteEvent(100, 1, MidiCommandCode.NoteOff, 96, 64), 3)),
            Recorded.OnNext(127, new MidiEventWithPort(new NoteOnEvent(100, 2, 95, 64, 666), 3)),
            Recorded.OnNext(128, new MidiEventWithPort(new NoteOnEvent(100, 1, 95, 64, 777), 3)),
            Recorded.OnNext(129, new MidiEventWithPort(new NoteEvent(100, 2, MidiCommandCode.NoteOff, 95, 64), 3)),
            Recorded.OnNext(130, new MidiEventWithPort(new NoteEvent(100, 1, MidiCommandCode.NoteOff, 95, 64), 3)),
        };

        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(20);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(30);

        var scheduler = new TestScheduler();
        var sut = CreateSplitAux(scheduler);

        var result2 = Record(sut, scheduler).SplitGroups;
        result2.Should()
            .BeEquivalentTo(
                new[]
                {
                    [
                        Recorded.Create(100, "P3 100 NoteOn Ch: 2 C8 Vel:64 Len: 444"),
                        Recorded.Create(101, "P3 100 NoteOn Ch: 1 C8 Vel:64 Len: 555"),
                        Recorded.Create(105, "P3 100 NoteOff Ch: 2 C8 Vel:64"),
                        Recorded.Create(106, "P3 100 NoteOff Ch: 1 C8 Vel:64"),
                        Recorded.Create(127, "P3 100 NoteOn Ch: 2 B7 Vel:64 Len: 666"),
                        Recorded.Create(128, "P3 100 NoteOn Ch: 1 B7 Vel:64 Len: 777"),
                        Recorded.Create(129, "P3 100 NoteOff Ch: 2 B7 Vel:64"),
                        Recorded.Create(130, "P3 100 NoteOff Ch: 1 B7 Vel:64")
                    ],
                    Array.Empty<Recorded<string>>()
                });

        MidiSplit<MidiEventWithPort> CreateSplitAux(TestScheduler testScheduler) =>
            CreateSplit(testScheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff, NAudioMidiEventAnalyzer.NoteAndSustainPedalCount);
    }

    private static MidiSplit<string> CreateSplit(
        TestScheduler scheduler,
        Recorded<Notification<string>>[] events,
        TimeSpan timeToSaveAfterHeldEvents,
        TimeSpan timeToSaveAfterAllOff)
    {
        return CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff, NoteAndSustainPedalCount);
    }
    
    private static MidiSplit<T> CreateSplit<T>(
        TestScheduler scheduler,
        Recorded<Notification<T>>[] events,
        TimeSpan timeToSaveAfterHeldEvents,
        TimeSpan timeToSaveAfterAllOff,
        Func<T, int> noteAndSustainPedalCount)
    {
        var allEvents = scheduler.CreateColdObservable(events);
        var split = MidiSplitter.Split(allEvents, noteAndSustainPedalCount, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff, scheduler);
        return split;
    }

    private static Recorded<string>[] PrepareResultBottom<T>(IObservable<T> result, TestScheduler scheduler) =>
        result
            .WaitAndGetRecorded(scheduler)
            .Select(x => new Recorded<string>(x.Time, x.Value?.ToString() ?? ""))
            .ToArray();

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

    public static Recorded<Notification<T>> Complete<T>(long time)
    {
        return Create(time, Notification.CreateOnCompleted<T>());
    }
}
