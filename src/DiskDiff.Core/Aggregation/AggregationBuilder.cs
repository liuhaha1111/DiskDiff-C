using DiskDiff.Core.Comparison;
using DiskDiff.Core.Models;
using DiskDiff.Core.Pathing;

namespace DiskDiff.Core.Aggregation;

public static class AggregationBuilder
{
    public static IReadOnlyDictionary<string, AggregatedNode> Build(
        IReadOnlyCollection<SnapshotEntry> currentEntries,
        IReadOnlyCollection<SnapshotComparisonRecord> diffs)
    {
        var nodes = new Dictionary<string, MutableAggregatedNode>(StringComparer.OrdinalIgnoreCase);
        var childPathsByDirectory = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in currentEntries)
        {
            RegisterEntryTopology(entry, nodes, childPathsByDirectory);

            if (entry.EntryType != EntryType.File)
            {
                continue;
            }

            foreach (var directoryPath in EnumerateOwningDirectories(entry.Path, entry.EntryType))
            {
                var node = EnsureNode(nodes, directoryPath);
                node.LogicalBytes += entry.LogicalBytes;
                node.AllocatedBytes += entry.AllocatedBytes;
            }
        }

        foreach (var diff in diffs.Where(diff => diff.ChangeType != ChangeType.Unchanged))
        {
            foreach (var directoryPath in EnumerateAncestorDirectories(diff.Path))
            {
                var node = EnsureNode(nodes, directoryPath);
                node.LogicalDelta += diff.LogicalDelta;
                node.AllocatedDelta += diff.AllocatedDelta;
                node.ChangedDescendantCount += 1;
            }
        }

        foreach (var pair in childPathsByDirectory)
        {
            EnsureNode(nodes, pair.Key).ChildCount = pair.Value.Count;
        }

        return nodes.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToImmutable(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static void RegisterEntryTopology(
        SnapshotEntry entry,
        IDictionary<string, MutableAggregatedNode> nodes,
        IDictionary<string, HashSet<string>> childPathsByDirectory)
    {
        foreach (var directoryPath in EnumerateOwningDirectories(entry.Path, entry.EntryType))
        {
            var currentPath = directoryPath;
            EnsureNode(nodes, currentPath);

            var parentPath = GetParentPath(currentPath);
            if (!string.IsNullOrEmpty(parentPath))
            {
                AddChild(childPathsByDirectory, parentPath, currentPath);
            }
        }

        if (!string.IsNullOrEmpty(entry.ParentPath))
        {
            AddChild(childPathsByDirectory, entry.ParentPath, entry.Path);
        }
    }

    private static MutableAggregatedNode EnsureNode(
        IDictionary<string, MutableAggregatedNode> nodes,
        string path)
    {
        var normalizedPath = WindowsPathNormalizer.Normalize(path);
        if (nodes.TryGetValue(normalizedPath, out var existing))
        {
            return existing;
        }

        var node = new MutableAggregatedNode(
            normalizedPath,
            GetParentPath(normalizedPath),
            GetName(normalizedPath));

        nodes[normalizedPath] = node;
        return node;
    }

    private static void AddChild(
        IDictionary<string, HashSet<string>> childPathsByDirectory,
        string parentPath,
        string childPath)
    {
        var normalizedParent = WindowsPathNormalizer.Normalize(parentPath);
        var normalizedChild = WindowsPathNormalizer.Normalize(childPath);

        if (!childPathsByDirectory.TryGetValue(normalizedParent, out var children))
        {
            children = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            childPathsByDirectory[normalizedParent] = children;
        }

        children.Add(normalizedChild);
    }

    private static IEnumerable<string> EnumerateOwningDirectories(string path, EntryType entryType)
    {
        var currentPath = WindowsPathNormalizer.Normalize(path);
        currentPath = entryType == EntryType.Directory
            ? currentPath
            : GetParentPath(currentPath);

        while (!string.IsNullOrEmpty(currentPath))
        {
            yield return currentPath;
            currentPath = GetParentPath(currentPath);
        }
    }

    private static IEnumerable<string> EnumerateAncestorDirectories(string path)
    {
        var currentPath = GetParentPath(WindowsPathNormalizer.Normalize(path));

        while (!string.IsNullOrEmpty(currentPath))
        {
            yield return currentPath;
            currentPath = GetParentPath(currentPath);
        }
    }

    private static string GetParentPath(string path)
    {
        var parent = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(parent))
        {
            return string.Empty;
        }

        var normalizedParent = WindowsPathNormalizer.Normalize(parent);
        return string.Equals(normalizedParent, path, StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : normalizedParent;
    }

    private static string GetName(string path)
    {
        var name = Path.GetFileName(path);
        return string.IsNullOrEmpty(name)
            ? path
            : name;
    }

    private sealed class MutableAggregatedNode
    {
        public MutableAggregatedNode(string path, string parentPath, string name)
        {
            Path = path;
            ParentPath = parentPath;
            Name = name;
        }

        public string Path { get; }

        public string ParentPath { get; }

        public string Name { get; }

        public long AllocatedBytes { get; set; }

        public long LogicalBytes { get; set; }

        public long AllocatedDelta { get; set; }

        public long LogicalDelta { get; set; }

        public int ChildCount { get; set; }

        public int ChangedDescendantCount { get; set; }

        public AggregatedNode ToImmutable() =>
            new(
                Path,
                ParentPath,
                Name,
                AllocatedBytes,
                LogicalBytes,
                AllocatedDelta,
                LogicalDelta,
                ChildCount,
                ChangedDescendantCount);
    }
}
