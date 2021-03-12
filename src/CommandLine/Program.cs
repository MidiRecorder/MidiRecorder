using System;
using System.IO.Abstractions;
using System.Linq;
using CannedBytes.Midi;
using MidiRecorder;

var midiInCaps = new MidiInPortCapsCollection();

foreach (var x in midiInCaps.Select((port, idx) => (port, idx)))
{
    Console.Write($"{x.idx}. {x.port.Name}");
}

var selectedIdx = int.Parse(Console.ReadLine());

var midiIn = new MidiInPort();
midiIn.Successor = new MyReceiver(new FileSystem());
midiIn.Open(selectedIdx);
midiIn.Start();

Console.ReadLine();

midiIn.Stop();
