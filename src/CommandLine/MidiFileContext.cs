using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CannedBytes.Midi.IO;

namespace MidiRecorder.CommandLine
{
    public class MidiFileContext
    {
        private readonly IEnumerable<MidiFileEvent> _eventList;
        private readonly DateTime _now;
        private readonly Guid _uniqueIdentifier;

        public MidiFileContext(IEnumerable<MidiFileEvent> eventList, DateTime now, Guid uniqueIdentifier)
        {
            _eventList = eventList;
            _now = now;
            _uniqueIdentifier = uniqueIdentifier;
        }

        internal string BuildFilePath(string formatString)
        {
            return StringExt.Format(formatString, new FormatData(
                _now,
                _eventList,
                _uniqueIdentifier
            ));
        }
    }

    internal class FormatData
    {
        private readonly ImmutableArray<MidiFileEvent> _eventList;
        private readonly Dictionary<string, object?> _memoStore = new();

        private T? Memoize<T>(string key, Func<T> expression)
        {
            if (!_memoStore.ContainsKey(key))
            {
                _memoStore.Add(key, expression());
            }

            return (T?)_memoStore[key];
        }

        public DateTime Now { get; }
        public int NumberOfEvents => _eventList.Length;
        public int NumberOfNoteEvents => Memoize(nameof(NumberOfNoteEvents), () => _eventList.Count(x => x.Message.GetData()[0] == 0x90));
        public Guid Guid { get; }

        public FormatData(DateTime now, IEnumerable<MidiFileEvent> eventList, Guid guid)
        {
            _eventList = eventList.ToImmutableArray();
            Now = now;
            Guid = guid;
        }

        public override bool Equals(object? obj)
        {
            return obj is FormatData other &&
                   Now == other.Now &&
                   NumberOfEvents == other.NumberOfEvents &&
                   Guid.Equals(other.Guid);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Now, NumberOfEvents, Guid);
        }
    }
}
