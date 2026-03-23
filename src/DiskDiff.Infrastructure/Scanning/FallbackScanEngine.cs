using DiskDiff.Core.Abstractions;
using DiskDiff.Core.Models;
using DiskDiff.Core.Pathing;

namespace DiskDiff.Infrastructure.Scanning;

public sealed class FallbackScanEngine : IScanEngine
{
    private readonly IScanFileSystem fileSystem;

    public FallbackScanEngine()
        : this(new SystemScanFileSystem())
    {
    }

    public FallbackScanEngine(IScanFileSystem fileSystem)
    {
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public Task<ScanResult> ScanAsync(ScanRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var snapshotId = Guid.NewGuid();
        var normalizedRoot = WindowsPathNormalizer.Normalize(request.DriveRoot);
        var startedAtUtc = DateTimeOffset.UtcNow;
        var entries = new List<SnapshotEntry>();
        var errors = new List<ScanErrorRecord>();

        var rootEnumerated = TryEnumerateDirectory(
            normalizedRoot,
            isRoot: true,
            snapshotId,
            entries,
            errors,
            cancellationToken);

        var completedAtUtc = DateTimeOffset.UtcNow;
        var completionState = !rootEnumerated
            ? CompletionState.Failed
            : errors.Count > 0
                ? CompletionState.Partial
                : CompletionState.Completed;

        var metadata = new SnapshotMetadata(
            snapshotId,
            normalizedRoot[..1],
            normalizedRoot,
            fileSystem.GetFileSystemType(normalizedRoot),
            "Fallback",
            startedAtUtc,
            completedAtUtc,
            entries.Count,
            entries.Where(entry => entry.EntryType == EntryType.File).Sum(entry => entry.LogicalBytes),
            entries.Where(entry => entry.EntryType == EntryType.File).Sum(entry => entry.AllocatedBytes),
            completionState,
            errors.Count);

        return Task.FromResult(new ScanResult(metadata, entries, errors));
    }

    private bool TryEnumerateDirectory(
        string directoryPath,
        bool isRoot,
        Guid snapshotId,
        ICollection<SnapshotEntry> entries,
        ICollection<ScanErrorRecord> errors,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ScanFileSystemEntry[] children;
        try
        {
            children = fileSystem.EnumerateEntries(directoryPath).ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
        {
            errors.Add(CreateError(snapshotId, directoryPath, exception));
            return false;
        }

        entries.Add(SnapshotEntry.Directory(directoryPath, snapshotId, attributes: FileAttributes.Directory));

        foreach (var child in children)
        {
            cancellationToken.ThrowIfCancellationRequested();

            entries.Add(ToSnapshotEntry(snapshotId, child));

            if (child.EntryType == EntryType.Directory
                && !child.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                TryEnumerateDirectory(child.Path, isRoot: false, snapshotId, entries, errors, cancellationToken);
            }
        }

        return true;
    }

    private static SnapshotEntry ToSnapshotEntry(Guid snapshotId, ScanFileSystemEntry entry) =>
        entry.EntryType == EntryType.Directory
            ? SnapshotEntry.Directory(entry.Path, snapshotId, entry.ModifiedAtUtc, entry.Attributes)
            : SnapshotEntry.File(
                entry.Path,
                entry.LogicalBytes,
                entry.AllocatedBytes,
                snapshotId,
                entry.ModifiedAtUtc,
                entry.Attributes);

    private static ScanErrorRecord CreateError(Guid snapshotId, string path, Exception exception) =>
        new(
            snapshotId,
            WindowsPathNormalizer.Normalize(path),
            "Enumerate",
            exception.GetType().Name,
            exception.Message);
}

public interface IScanFileSystem
{
    IEnumerable<ScanFileSystemEntry> EnumerateEntries(string directoryPath);

    string GetFileSystemType(string rootPath);
}

public sealed record ScanFileSystemEntry(
    string Path,
    EntryType EntryType,
    long LogicalBytes,
    long AllocatedBytes,
    DateTimeOffset? ModifiedAtUtc,
    FileAttributes Attributes)
{
    public static ScanFileSystemEntry Directory(
        string path,
        FileAttributes attributes = FileAttributes.Directory,
        DateTimeOffset? modifiedAtUtc = null) =>
        new(
            WindowsPathNormalizer.Normalize(path),
            EntryType.Directory,
            0,
            0,
            modifiedAtUtc,
            attributes);

    public static ScanFileSystemEntry File(
        string path,
        long length,
        DateTimeOffset? modifiedAtUtc = null,
        FileAttributes attributes = FileAttributes.Normal) =>
        new(
            WindowsPathNormalizer.Normalize(path),
            EntryType.File,
            length,
            length,
            modifiedAtUtc,
            attributes);
}

internal sealed class SystemScanFileSystem : IScanFileSystem
{
    public IEnumerable<ScanFileSystemEntry> EnumerateEntries(string directoryPath)
    {
        var directoryInfo = new DirectoryInfo(directoryPath);

        foreach (var entry in directoryInfo.EnumerateFileSystemInfos())
        {
            var modifiedAtUtc = entry.LastWriteTimeUtc == DateTime.MinValue
                ? (DateTimeOffset?)null
                : new DateTimeOffset(entry.LastWriteTimeUtc, TimeSpan.Zero);
            if (entry.Attributes.HasFlag(FileAttributes.Directory))
            {
                yield return ScanFileSystemEntry.Directory(entry.FullName, entry.Attributes, modifiedAtUtc);
                continue;
            }

            var fileInfo = (FileInfo)entry;
            yield return ScanFileSystemEntry.File(entry.FullName, fileInfo.Length, modifiedAtUtc, entry.Attributes);
        }
    }

    public string GetFileSystemType(string rootPath)
    {
        try
        {
            var driveRoot = Path.GetPathRoot(rootPath);
            return string.IsNullOrEmpty(driveRoot)
                ? "Unknown"
                : new DriveInfo(driveRoot).DriveFormat;
        }
        catch (IOException)
        {
            return "Unknown";
        }
    }
}


