namespace DiskDiff.Core.Models;

public sealed record ScanErrorRecord(
    Guid SnapshotId,
    string Path,
    string Phase,
    string ErrorCode,
    string Message);
