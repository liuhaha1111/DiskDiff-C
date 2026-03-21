namespace DiskDiff.Core.Models;

public sealed record SnapshotMetadata(
    Guid SnapshotId,
    string DriveLetter,
    string RootPath,
    string FileSystemType,
    string ScanMode,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    long ItemCount,
    long TotalLogicalBytes,
    long TotalAllocatedBytes,
    CompletionState CompletionState,
    int ErrorCount);
