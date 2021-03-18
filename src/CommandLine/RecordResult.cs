using System;

namespace MidiRecorder.CommandLine
{
    public class RecordResult : IDisposable
    {
        private readonly Action? _action;

        public RecordResult(Action action)
        {
            _action = action;
            ErrorMessage = null;
        }

        public RecordResult(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public string? ErrorMessage { get; }
        public bool IsError => ErrorMessage != null;

        public void Dispose()
        {
            _action?.Invoke();
        }
    }
}