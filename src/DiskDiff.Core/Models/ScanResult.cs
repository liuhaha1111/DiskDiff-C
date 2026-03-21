namespace DiskDiff.Core.Models;

public sealed record ScanResult(
    SnapshotMetadata Metadata,
    IReadOnlyList<SnapshotEntry> Entries,
    IReadOnlyList<ScanErrorRecord> Errors);
