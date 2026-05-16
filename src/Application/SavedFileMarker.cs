namespace MidiRecorder.Application;

public static class SavedFileMarker
{
    private static readonly char[] DisallowedMarkerChars =
        ['/', '\\', ':', '*', '?', '"', '<', '>', '|', .. Path.GetInvalidFileNameChars()];

    public static string ApplySuffix(string filePath, string suffix)
    {
        var directory = Path.GetDirectoryName(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        var markedName = fileName + suffix + extension;
        return string.IsNullOrEmpty(directory) ? markedName : Path.Combine(directory, markedName);
    }

    public static bool TryValidateSuffix(string? suffix, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(suffix))
        {
            errorMessage = "Marker suffix cannot be empty.";
            return false;
        }

        if (suffix.IndexOfAny(DisallowedMarkerChars) >= 0)
        {
            errorMessage =
                $"Marker suffix '{suffix}' contains invalid filename characters.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
