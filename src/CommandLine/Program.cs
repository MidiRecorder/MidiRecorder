using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CannedBytes.Midi;
using CannedBytes.Midi.IO;
using CannedBytes.Midi.Message;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;
using MidiRecorder.CommandLine;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddFilter("Microsoft", LogLevel.Warning)
        .AddFilter("System", LogLevel.Warning)
        .AddFilter("MidiRecorder", LogLevel.Trace)
        .AddSimpleConsole(c => 
            c.SingleLine = true);
});
ILogger logger = loggerFactory.CreateLogger("MidiRecorder");

using var parser = new Parser(with =>
{
    with.HelpWriter = null;
});

ParserResult<object> parserResult = parser.ParseArguments<RecordOptions, ListMidiInputsOptions, TestFormatOptions>(args);

return parserResult
    .MapResult<RecordOptions, ListMidiInputsOptions, TestFormatOptions, int>(
        Record,
        ListMidiInputs,
        TestFormat,
        errors => DisplayHelp(parserResult, errors));

int ListMidiInputs(ListMidiInputsOptions options)
{
    var midiInCapabilities = new MidiInPortCapsCollection();

    if (midiInCapabilities.Count == 0)
    {
        logger.LogError("No MIDI inputs.");
    }

    foreach (var x in midiInCapabilities.Select((port, idx) => (port, idx)))
    {
        Console.WriteLine($"{x.idx}. {x.port.Name}");
    }

    return 0;
}

int Record(RecordOptions options)
{
    var svc = new MidiRecorderApplicationService(logger);

    using RecordResult stop = svc.StartRecording(options);

    if (stop.IsError)
    {
        Console.WriteLine(stop.ErrorMessage);
        return 1;
    }

    Console.WriteLine("Recording started. Press any key to quit.");
    Console.ReadLine();
    return 0;
}

int TestFormat(TestFormatOptions options)
{
    try
    {
        string filePath = FormatTester.TestFormat(options.PathFormatString);
        Console.WriteLine(filePath);
    }
    catch (Exception ex)
    {
        Console.WriteLine("ERROR: " + ex.Message);
    }

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
