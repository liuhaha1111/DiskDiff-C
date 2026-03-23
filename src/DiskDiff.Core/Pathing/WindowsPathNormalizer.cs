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

        if (fullPath.Length >= 2 && fullPath[1] == ':')
        {
            fullPath = char.ToUpperInvariant(fullPath[0]) + fullPath[1..];
        }

        if (IsDriveRoot(fullPath))
        {
            return fullPath;
        }

        return fullPath.TrimEnd('\\');
    }

    private static bool IsDriveRoot(string path)
    {
        return path.Length == 3 && char.IsLetter(path[0]) && path[1] == ':' && path[2] == '\\';
    }
}
