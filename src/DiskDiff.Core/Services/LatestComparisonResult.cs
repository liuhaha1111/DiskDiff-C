using DiskDiff.Core.Aggregation;
using DiskDiff.Core.Comparison;
using DiskDiff.Core.Models;

namespace DiskDiff.Core.Services;

public sealed record LatestComparisonResult(
    SnapshotMetadata? LatestSnapshot,
    SnapshotMetadata? PreviousSnapshot,
    IReadOnlyList<SnapshotEntry> CurrentEntries,
    IReadOnlyList<SnapshotComparisonRecord> ComparisonRecords,
    IReadOnlyDictionary<string, AggregatedNode> AggregatedNodes)
{
    public static LatestComparisonResult Empty { get; } = new(
        null,
        null,
        Array.Empty<SnapshotEntry>(),
        Array.Empty<SnapshotComparisonRecord>(),
        new Dictionary<string, AggregatedNode>(StringComparer.OrdinalIgnoreCase));
}
