using System;
using System.Collections.Immutable;
using CannedBytes.Midi.IO;

namespace MidiRecorder
{
    public class MidiFileContext
    {
        private ImmutableList<MidiFileEvent> _eventList;
        private DateTime _now;

        public MidiFileContext(ImmutableList<MidiFileEvent> eventList, DateTime now)
        {
            _eventList = eventList;
            _now = now;
        }

        internal string BuildFilePath(string formatString)
        {
            return StringEx.Format(formatString, new {
                Now = _now,
                NumberOfEvents = _eventList.Count
            });
        }
    }        
}
