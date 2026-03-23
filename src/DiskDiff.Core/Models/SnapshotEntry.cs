using DiskDiff.Core.Pathing;

namespace DiskDiff.Core.Models;

public sealed record SnapshotEntry(
    Guid SnapshotId,
    string Path,
    string ParentPath,
    string Name,
    EntryType EntryType,
    long LogicalBytes,
    long AllocatedBytes,
    DateTimeOffset? ModifiedAtUtc,
    FileAttributes Attributes)
{
    public static SnapshotEntry File(
        string path,
        long logicalBytes,
        long allocatedBytes,
        Guid? snapshotId = null,
        DateTimeOffset? modifiedAtUtc = null,
        FileAttributes attributes = FileAttributes.Normal)
    {
        var normalizedPath = WindowsPathNormalizer.Normalize(path);

        return new SnapshotEntry(
            snapshotId ?? Guid.Empty,
            normalizedPath,
            GetParentPath(normalizedPath),
            GetName(normalizedPath),
            EntryType.File,
            logicalBytes,
            allocatedBytes,
            modifiedAtUtc,
            attributes);
    }

    public static SnapshotEntry Directory(
        string path,
        Guid? snapshotId = null,
        DateTimeOffset? modifiedAtUtc = null,
        FileAttributes attributes = FileAttributes.Directory)
    {
        var normalizedPath = WindowsPathNormalizer.Normalize(path);

        return new SnapshotEntry(
            snapshotId ?? Guid.Empty,
            normalizedPath,
            GetParentPath(normalizedPath),
            GetName(normalizedPath),
            EntryType.Directory,
            0,
            0,
            modifiedAtUtc,
            attributes);
    }

    private static string GetParentPath(string normalizedPath)
    {
        var parent = global::System.IO.Path.GetDirectoryName(normalizedPath);
        if (string.IsNullOrEmpty(parent))
        {
            return string.Empty;
        }

        var normalizedParent = WindowsPathNormalizer.Normalize(parent);
        return string.Equals(normalizedParent, normalizedPath, StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : normalizedParent;
    }

    private static string GetName(string normalizedPath)
    {
        var name = global::System.IO.Path.GetFileName(normalizedPath);
        return string.IsNullOrEmpty(name)
            ? normalizedPath
            : name;
    }
}
