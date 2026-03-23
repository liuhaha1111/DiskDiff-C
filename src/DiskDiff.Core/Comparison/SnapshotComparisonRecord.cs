using DiskDiff.Core.Models;

namespace DiskDiff.Core.Comparison;

public sealed record SnapshotComparisonRecord(
    string Path,
    string ParentPath,
    string Name,
    EntryType EntryType,
    ChangeType ChangeType,
    long CurrentLogicalBytes,
    long PreviousLogicalBytes,
    long CurrentAllocatedBytes,
    long PreviousAllocatedBytes,
    long LogicalDelta,
    long AllocatedDelta);
