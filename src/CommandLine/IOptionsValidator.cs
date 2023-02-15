using MidiRecorder.Application;

namespace MidiRecorder.CommandLine;

public interface IOptionsValidator
{
    (TypedRecordOptions? typedRecordOptions, string errorMessage) Validate(RecordOptions options);
}
