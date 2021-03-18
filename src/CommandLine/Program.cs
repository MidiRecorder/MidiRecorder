using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CannedBytes.Midi;
using CommandLine;
using CommandLine.Text;
using MidiRecorder;
using MidiRecorder.CommandLine;

if (args.Length == 0)
{
    args = new[] { "--help" };
}

using var parser = new Parser(with =>
{
    with.HelpWriter = null;
});

var parserResult = parser.ParseArguments<RecordOptions, ListMidiInputsOptions>(args);

return parserResult
    .MapResult<RecordOptions, ListMidiInputsOptions, int>(
        Record,
        ListMidiInputs,
        errors => DisplayHelp(parserResult, errors));

int ListMidiInputs(ListMidiInputsOptions options)
{
    var midiInCapabilities = new MidiInPortCapsCollection();

    if (midiInCapabilities.Count == 0)
    {
        Console.WriteLine("No MIDI inputs.");
    }

    foreach (var x in midiInCapabilities.Select((port, idx) => (port, idx)))
    {
        Console.WriteLine($"{x.idx}. {x.port.Name}");
    }

    return 0;
}

int Record(RecordOptions options)
{
    var svc = new MidiRecorderApplicationService();

    using RecordResult stop = svc.StartRecording(options);

    if (stop.IsError)
    {
        DisplayHelp(parserResult, Enumerable.Empty<Error>());
        return 1;
    }

    Console.WriteLine("Recording started. Press any key to quit.");
    Console.ReadLine();
    return 0;
}

static int DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errors)
{
    var errs = errors.ToArray();

    if (errs.IsVersion())
    {
        var helpText = HelpText.AutoBuild(result);
        Console.WriteLine(helpText);
        return 0;
    }
    if (errs.IsHelp())
    {
        Console.WriteLine(GetHelpText(verbs: true));
        return 0;
    }

    Console.WriteLine(GetHelpText(verbs: false));
    return 1;

    string GetHelpText(bool verbs)
    {
        return HelpText.AutoBuild(result, h =>
            {
                h.AdditionalNewLineAfterOption = false;
                string? assemblyDescription = Assembly.GetExecutingAssembly()
                    .GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)
                    .OfType<AssemblyDescriptionAttribute>()
                    .FirstOrDefault()
                    ?.Description;
                if (errs.IsHelp())
                    h.AddPreOptionsLine(assemblyDescription);
                return HelpText.DefaultParsingErrorsHandler(result, h);
            },
            e => e,
            verbsIndex: verbs);
    }
}
