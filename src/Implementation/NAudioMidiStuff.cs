using MidiRecorder.Application.Record;

namespace MidiRecorder.Application.Implementation;

public class NAudioMidiTools : MidiTools<NAudioMidiEvent>
{
    public override IEnumerable<MidiInput> GetMidiInputs() =>
        NAudioMidiInputs.GetMidiInputs();

    public override IEnumerable<MidiInput> SearchMidiInputId(string midiInputName) =>
        NAudioMidiInputs.SearchMidiInputId(midiInputName);

    public override IMidiSource<NAudioMidiEvent> BuildMidiSource(IEnumerable<MidiInput> midiInput) =>
        new NAudioMidiSource(midiInput);

    public override IEnumerable<IEnumerable<NAudioMidiEvent>> BuildTracks(IEnumerable<NAudioMidiEvent> events) =>
        NAudioMidiTrackBuilder.BuildTracks(events);

    public override void SaveFile(IEnumerable<IEnumerable<NAudioMidiEvent>> tracks, string filePath, int timePrecision) =>
        NAudioMidiFiles.Save(tracks, filePath, timePrecision);

    public override IEnumerable<IEnumerable<NAudioMidiEvent>> OpenFile(string filePath, int port) =>
        NAudioMidiFiles.Open(filePath, port);
}
