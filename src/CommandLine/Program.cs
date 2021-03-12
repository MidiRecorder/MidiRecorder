using System;
using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
using CannedBytes.Midi;
using MidiRecorder;

var midiInCaps = new MidiInPortCapsCollection();

if (midiInCaps.Count == 0)
{
    Console.WriteLine("You need a MIDI input to use this tool.");
    return;
}

foreach (var x in midiInCaps.Select((port, idx) => (port, idx)))
{
    Console.Write($"{x.idx}. {x.port.Name}");
}

var selectedIdx = int.Parse(Console.ReadLine() ?? "", CultureInfo.InvariantCulture);

using var midiIn = new MidiInPort();
midiIn.Successor = new RecordingReceiver(new FileSystem());
midiIn.Open(selectedIdx);
midiIn.Start();

Console.ReadLine();

midiIn.Stop();
