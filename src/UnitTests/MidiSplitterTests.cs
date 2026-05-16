using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MidiRecorder.Application;
using MidiRecorder.Application.Implementation;
using NAudio.Midi;

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
        result2.Should()
            .BeEquivalentTo(
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
        result2.Should()
            .BeEquivalentTo(
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
        result2.Should()
            .BeEquivalentTo(
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
        result2.Should()
            .BeEquivalentTo(
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
        result2.Should()
            .BeEquivalentTo(
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

    [TestMethod(DisplayName = "Regression for #17")]
    public void Split_NoteOffCommands_SingleGroup()
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

        var timeoutToSave = TimeSpan.FromTicks(30);
        var delayToSave = TimeSpan.FromTicks(15);
        var scheduler = new TestScheduler();
        var analyzer = new NAudioMidiEventAnalyzer();
        var sut = CreateSplit(scheduler, events, timeoutToSave, delayToSave, analyzer.NoteAndSustainPedalCount);

        var result2 = PrepareResult(sut.SplitGroups, scheduler);
        result2.Should()
            .BeEquivalentTo(
                new[]
                {
                    new[]
                    {
                        Recorded.Create(101, "P3 100 NoteOn Ch: 2 C8 Vel:64 Len: 444"),
                        Recorded.Create(102, "P3 100 NoteOn Ch: 1 C8 Vel:64 Len: 555"),
                        Recorded.Create(106, "P3 100 NoteOff Ch: 2 C8 Vel:64"),
                        Recorded.Create(107, "P3 100 NoteOff Ch: 1 C8 Vel:64"),
                        Recorded.Create(111, "P3 100 NoteOn Ch: 2 B7 Vel:64 Len: 666"),
                        Recorded.Create(112, "P3 100 NoteOn Ch: 1 B7 Vel:64 Len: 777"),
                        Recorded.Create(113, "P3 100 NoteOff Ch: 2 B7 Vel:64"),
                        Recorded.Create(114, "P3 100 NoteOff Ch: 1 B7 Vel:64")
                    }
                });
    }

    [TestMethod(DisplayName = "Regression for #17")]
    public void Split_NoteOffCommands_SplitsOnHeldTimeout()
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

        var timeoutToSave = TimeSpan.FromTicks(20);
        var delayToSave = TimeSpan.FromTicks(30);
        var scheduler = new TestScheduler();
        var analyzer = new NAudioMidiEventAnalyzer();
        var sut = CreateSplit(scheduler, events, timeoutToSave, delayToSave, analyzer.NoteAndSustainPedalCount);

        var result2 = PrepareResult(sut.SplitGroups, scheduler);
        result2.Should()
            .BeEquivalentTo(
                new[]
                {
                    new[]
                    {
                        Recorded.Create(101, "P3 100 NoteOn Ch: 2 C8 Vel:64 Len: 444"),
                        Recorded.Create(102, "P3 100 NoteOn Ch: 1 C8 Vel:64 Len: 555"),
                        Recorded.Create(106, "P3 100 NoteOff Ch: 2 C8 Vel:64"),
                        Recorded.Create(107, "P3 100 NoteOff Ch: 1 C8 Vel:64"),
                        Recorded.Create(128, "P3 100 NoteOn Ch: 2 B7 Vel:64 Len: 666"),
                        Recorded.Create(129, "P3 100 NoteOn Ch: 1 B7 Vel:64 Len: 777"),
                        Recorded.Create(130, "P3 100 NoteOff Ch: 2 B7 Vel:64"),
                        Recorded.Create(131, "P3 100 NoteOff Ch: 1 B7 Vel:64")
                    }
                });
    }

    private static MidiSplit<string> CreateSplit(
        TestScheduler scheduler,
        Recorded<Notification<string>>[] events,
        TimeSpan timeoutToSave,
        TimeSpan delayToSave)
    {
        return CreateSplit(scheduler, events, timeoutToSave, delayToSave, NoteAndSustainPedalCount);
    }

    private static MidiSplit<T> CreateSplit<T>(
        TestScheduler scheduler,
        Recorded<Notification<T>>[] events,
        TimeSpan timeoutToSave,
        TimeSpan delayToSave,
        Func<T, int> noteAndSustainPedalCount)
    {
        var allEvents = scheduler.CreateColdObservable(events);
        var sut = new MidiSplitter<T>(scheduler);
        return sut.Split(allEvents, noteAndSustainPedalCount, timeoutToSave, delayToSave);
    }

    private static Recorded<string>[][] PrepareResult<T>(IObservable<IObservable<T>> result, TestScheduler scheduler)
    {
        return result.SelectMany((observable, index) => observable.Select(x => (index, x)))
            .WaitAndGetRecorded(scheduler)
            .GroupBy(x => x.Value.index, x => Recorded.Create(x.Time, x.Value.x?.ToString() ?? ""))
            .Select(x => x.ToArray())
            .ToArray();
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
