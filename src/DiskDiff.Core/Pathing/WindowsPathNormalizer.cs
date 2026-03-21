namespace DiskDiff.Core.Pathing;

public static class WindowsPathNormalizer
{
    public static string Normalize(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            throw new ArgumentException("Path cannot be null or whitespace.", nameof(rawPath));
        }

        var fullPath = Path.GetFullPath(rawPath).Replace('/', '\\');

        if (IsDriveRoot(fullPath))
        {
            return $"{char.ToUpperInvariant(fullPath[0])}:\\";
        }

        var trimmedPath = fullPath.TrimEnd('\\');

        if (trimmedPath.Length >= 2 && trimmedPath[1] == ':')
        {
            var drivePrefix = $"{char.ToUpperInvariant(trimmedPath[0])}:";
            var remainder = trimmedPath[2..];
            var normalizedRemainder = NormalizeSegments(remainder);
            return drivePrefix + normalizedRemainder;
        }

        return trimmedPath;
    }

    private static bool IsDriveRoot(string path)
    {
        return path.Length == 3 && char.IsLetter(path[0]) && path[1] == ':' && path[2] == '\\';
    }

    private static string NormalizeSegments(string remainder)
    {
        if (string.IsNullOrEmpty(remainder))
        {
            return "\\";
        }

        var segments = remainder
            .Split('\\', StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeSegment);

        return "\\" + string.Join("\\", segments);
    }

    private static string NormalizeSegment(string segment)
    {
        if (segment.Length == 0)
        {
            return segment;
        }

        if (!char.IsLetter(segment[0]))
        {
            return segment;
        }

        if (segment.Length == 1)
        {
            return char.ToUpperInvariant(segment[0]).ToString();
        }

        return char.ToUpperInvariant(segment[0]) + segment[1..].ToLowerInvariant();
    }
}
