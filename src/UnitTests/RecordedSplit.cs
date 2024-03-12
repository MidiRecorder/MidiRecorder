using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using LanguageExt;
using Microsoft.Reactive.Testing;
using MidiRecorder.Application;

namespace MidiRecorder.Tests;

public class RecordedSplit<T>
{
    public RecordedSplit(MidiSplit<T> sut, TestScheduler scheduler)
    {
        var splitGroups = new List<(long, List<Recorded<T>>)>();
        sut.SplitGroups.Timestamp(scheduler).Subscribe(
            o =>
            {
                var groupList = new List<Recorded<T>>();
                splitGroups.Add((o.Timestamp.Ticks, groupList));
                o.Value.Timestamp(scheduler).Select(it => Recorded.Create(it.Timestamp.Ticks, it.Value)).Subscribe(groupList.Add);
            });
        var savingPoints = new List<Recorded<Unit>>();
        
        sut.SavingPoints.Timestamp(scheduler).Select(it => Recorded.Create(it.Timestamp.Ticks, it.Value))
            .Subscribe(savingPoints.Add);
        
        scheduler.Start();

        SplitGroups = splitGroups.OrderBy(x => x.Item1).Select(g => g.Item2.Select(o => Recorded.Create(o.Time, o.Value?.ToString() ?? "")).ToArray()).ToArray();
        SavingPoints = savingPoints.ToArray();
    }

    public Recorded<Unit>[] SavingPoints { get; }

    public Recorded<string>[][] SplitGroups { get; }
}
