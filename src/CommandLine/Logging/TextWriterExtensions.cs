namespace MidiRecorder.CommandLine.Logging;

internal static class TextWriterExtensions
{
    private const string DefaultForegroundColor = "\u001B[39m\u001B[22m";
    private const string DefaultBackgroundColor = "\u001B[49m";

    public static void SetForegroundColor(this TextWriter textWriter, ConsoleColor? foreground)
    {
        if (foreground.HasValue)
        {
            textWriter.Write(foreground.Value.GetForegroundColorEscapeCode());
        }
    }

    public static void ResetForegroundColor(this TextWriter textWriter, ConsoleColor? foreground)
    {
        if (foreground.HasValue)
        {
            textWriter.Write(DefaultForegroundColor); // reset to default foreground color
        }
    }

    public static void WriteColoredMessage(
        this TextWriter textWriter,
        string message,
        ConsoleColor? background,
        ConsoleColor? foreground)
    {
        // Order: background color, foreground color, Message, reset foreground color, reset background color
        if (background.HasValue)
        {
            textWriter.Write(background.Value.GetBackgroundColorEscapeCode());
        }

        if (foreground.HasValue)
        {
            textWriter.Write(foreground.Value.GetForegroundColorEscapeCode());
        }

        textWriter.Write(message);
        if (foreground.HasValue)
        {
            textWriter.Write(DefaultForegroundColor); // reset to default foreground color
        }

        if (background.HasValue)
        {
            textWriter.Write(DefaultBackgroundColor); // reset to the background color
        }
    }

    private static string GetForegroundColorEscapeCode(this ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "\u001B[30m",
            ConsoleColor.DarkBlue => "\u001B[34m",
            ConsoleColor.DarkGreen => "\u001B[32m",
            ConsoleColor.DarkCyan => "\u001B[36m",
            ConsoleColor.DarkRed => "\u001B[31m",
            ConsoleColor.DarkMagenta => "\u001B[35m",
            ConsoleColor.DarkYellow => "\u001B[33m",
            ConsoleColor.Gray => "\u001B[37m",
            ConsoleColor.Blue => "\u001B[1m\u001B[34m",
            ConsoleColor.Green => "\u001B[1m\u001B[32m",
            ConsoleColor.Cyan => "\u001B[1m\u001B[36m",
            ConsoleColor.Red => "\u001B[1m\u001B[31m",
            ConsoleColor.Magenta => "\u001B[1m\u001B[35m",
            ConsoleColor.Yellow => "\u001B[1m\u001B[33m",
            ConsoleColor.White => "\u001B[1m\u001B[37m",
            _ => "\u001B[39m\u001B[22m"
        };
    }

    private static string GetBackgroundColorEscapeCode(this ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "\u001B[40m",
            ConsoleColor.DarkBlue => "\u001B[44m",
            ConsoleColor.DarkGreen => "\u001B[42m",
            ConsoleColor.DarkCyan => "\u001B[46m",
            ConsoleColor.DarkRed => "\u001B[41m",
            ConsoleColor.DarkMagenta => "\u001B[45m",
            ConsoleColor.DarkYellow => "\u001B[43m",
            ConsoleColor.Gray => "\u001B[47m",
            _ => "\u001B[49m"
        };
    }
}
