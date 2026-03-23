namespace DiskDiff.Core.Aggregation;

public sealed record AggregatedNode(
    string Path,
    string ParentPath,
    string Name,
    long AllocatedBytes,
    long LogicalBytes,
    long AllocatedDelta,
    long LogicalDelta,
    int ChildCount,
    int ChangedDescendantCount);
