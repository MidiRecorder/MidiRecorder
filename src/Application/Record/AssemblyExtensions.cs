using System.Reflection;

namespace MidiRecorder.Application.Record;

public static class AssemblyExtensions
{
    public static TAttribute? Get<TAttribute>() where TAttribute : Attribute
    {
        return Assembly.GetEntryAssembly()
            ?.GetCustomAttributes(typeof(TAttribute), false)
            .OfType<TAttribute>()
            .FirstOrDefault();
    }
}
