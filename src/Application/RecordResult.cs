namespace MidiRecorder.Application;

public sealed class RecordResult : IDisposable
{
    private readonly Action? _disposeAction;

    public RecordResult(Action disposeAction)
    {
        _disposeAction = disposeAction;
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
        _disposeAction?.Invoke();
    }
}
