using DiskDiff.Core.Models;
using DiskDiff.Core.Pathing;

namespace DiskDiff.Core.Comparison;

public static class SnapshotComparisonEngine
{
    public static IReadOnlyList<SnapshotComparisonRecord> Compare(
        IReadOnlyCollection<SnapshotEntry> previousEntries,
        IReadOnlyCollection<SnapshotEntry> currentEntries)
    {
        var previousByPath = BuildIndex(previousEntries);
        var currentByPath = BuildIndex(currentEntries);

        var allPaths = new HashSet<string>(previousByPath.Keys, StringComparer.OrdinalIgnoreCase);
        allPaths.UnionWith(currentByPath.Keys);

        var result = new List<SnapshotComparisonRecord>(allPaths.Count);

        foreach (var path in allPaths.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            previousByPath.TryGetValue(path, out var previousEntry);
            currentByPath.TryGetValue(path, out var currentEntry);

            var source = currentEntry ?? previousEntry
                ?? throw new InvalidOperationException("Comparison source entry is missing.");

            var currentLogical = currentEntry?.LogicalBytes ?? 0;
            var previousLogical = previousEntry?.LogicalBytes ?? 0;
            var currentAllocated = currentEntry?.AllocatedBytes ?? 0;
            var previousAllocated = previousEntry?.AllocatedBytes ?? 0;

            var changeType = ResolveChangeType(previousEntry, currentEntry);

            result.Add(new SnapshotComparisonRecord(
                source.Path,
                source.ParentPath,
                source.Name,
                source.EntryType,
                changeType,
                currentLogical,
                previousLogical,
                currentAllocated,
                previousAllocated,
                currentLogical - previousLogical,
                currentAllocated - previousAllocated));
        }

        return result;
    }

    private static Dictionary<string, SnapshotEntry> BuildIndex(IReadOnlyCollection<SnapshotEntry> entries)
    {
        var index = new Dictionary<string, SnapshotEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var normalizedKey = WindowsPathNormalizer.Normalize(entry.Path);
            index[normalizedKey] = entry;
        }

        return index;
    }

    private static ChangeType ResolveChangeType(SnapshotEntry? previousEntry, SnapshotEntry? currentEntry)
    {
        if (previousEntry is null && currentEntry is not null)
        {
            return ChangeType.Added;
        }

        if (previousEntry is not null && currentEntry is null)
        {
            return ChangeType.Deleted;
        }

        if (previousEntry is null || currentEntry is null)
        {
            throw new InvalidOperationException("Comparison entries cannot both be null.");
        }

        if (previousEntry.EntryType != currentEntry.EntryType)
        {
            return ChangeType.SizeChanged;
        }

        if (previousEntry.LogicalBytes != currentEntry.LogicalBytes
            || previousEntry.AllocatedBytes != currentEntry.AllocatedBytes)
        {
            return ChangeType.SizeChanged;
        }

        return ChangeType.Unchanged;
    }
}
