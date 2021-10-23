using System;

namespace MidiRecorder.CommandLine
{
    public class RecordStartResult : IDisposable
    {
        private readonly Action? _action;

        public RecordStartResult(Action action)
        {
            _action = action;
            ErrorMessage = null;
        }

        public RecordStartResult(string errorMessage)
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