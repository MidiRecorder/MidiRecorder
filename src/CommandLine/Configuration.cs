using System.ComponentModel;
using Args;

class Configuration
{
    public Configuration(string input, long delayToSave, string pathFormatString)
    {
        Input = input;
        DelayToSave = delayToSave;
        PathFormatString = pathFormatString;
    }

    [Description("MIDI Input Name")]
    public string Input { get; }


    [Description("Delay before saving the latest recorded MIDI events")]
    public long DelayToSave { get; }

    [ArgsMemberSwitch("f")]
    public string PathFormatString { get;}
}
