using DiskDiff.Core.Aggregation;
using DiskDiff.Core.Comparison;
using DiskDiff.Core.Models;
using FluentAssertions;

namespace DiskDiff.Core.Tests.Aggregation;

public sealed class AggregationBuilderTests
{
    [Fact]
    public void Build_rolls_file_sizes_to_each_parent_directory()
    {
        var entries = new[]
        {
            SnapshotEntry.File("C:\\Users\\Admin\\a.bin", 100, 100),
        };

        var nodes = AggregationBuilder.Build(entries, Array.Empty<SnapshotComparisonRecord>());

        nodes["C:\\Users"].LogicalBytes.Should().Be(100);
        nodes["C:\\Users\\Admin"].LogicalBytes.Should().Be(100);
    }

    [Fact]
    public void Build_counts_changed_descendants_when_a_child_file_changes()
    {
        var entries = new[]
        {
            SnapshotEntry.File("C:\\Users\\Admin\\a.bin", 100, 100),
        };
        var diffs = new[]
        {
            new SnapshotComparisonRecord(
                "C:\\Users\\Admin\\a.bin",
                "C:\\Users\\Admin",
                "a.bin",
                EntryType.File,
                ChangeType.SizeChanged,
                100,
                80,
                100,
                80,
                20,
                20),
        };

        var nodes = AggregationBuilder.Build(entries, diffs);

        nodes["C:\\Users"].ChangedDescendantCount.Should().BeGreaterThan(0);
        nodes["C:\\Users\\Admin"].ChangedDescendantCount.Should().BeGreaterThan(0);
    }
}
