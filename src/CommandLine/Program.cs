using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using CannedBytes.Midi;
using MidiRecorder;

Configuration configuration = Args.Configuration.Configure<Configuration>().CreateAndBind(args);

var selectedIndex = GetMidiInputIndex(configuration);
var delayToSave = TimeSpan.FromMilliseconds(configuration.DelayToSave);
var pathFormatString = configuration.PathFormatString ?? throw new ArgumentNullException("PathFormatString");

var svc = new MidiRecorderApplicationService();

using var stop = svc.StartRecording(selectedIndex, delayToSave, pathFormatString);

Console.WriteLine("Recording started. Press any key to quit.");
Console.ReadLine();

int GetMidiInputIndex(Configuration configuration)
{

    var midiInCapabilities = new MidiInPortCapsCollection();

    if (midiInCapabilities.Count == 0)
    {
        throw new Exception("You need a MIDI input to use this tool.");
    }

    int? selectedIdx = midiInCapabilities
        .Select((port, idx) => ((MidiInPortCaps port, int idx)?)(port, idx))
        .FirstOrDefault(x => string.Equals(x.Value.port.Name, configuration.Input, StringComparison.OrdinalIgnoreCase))?.idx;

    if (selectedIdx == null)
    {
        foreach (var x in midiInCapabilities.Select((port, idx) => (port, idx)))
        {
            Console.WriteLine($"{x.idx}. {x.port.Name}");
        }

        Console.Write("Choose MIDI input: ");
        selectedIdx = int.Parse(Console.ReadLine() ?? "", CultureInfo.InvariantCulture);
    }

    Console.WriteLine("The chosen MIDI input is " + midiInCapabilities[selectedIdx.Value].Name);

    return selectedIdx.Value;
}