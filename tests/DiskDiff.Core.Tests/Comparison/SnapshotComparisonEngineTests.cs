using DiskDiff.Core.Comparison;
using DiskDiff.Core.Models;
using FluentAssertions;

namespace DiskDiff.Core.Tests.Comparison;

public sealed class SnapshotComparisonEngineTests
{
    [Fact]
    public void Compare_marks_missing_new_path_as_deleted()
    {
        var previous = new[] { SnapshotEntry.File("C:\\temp\\a.bin", 10, 10) };
        var current = Array.Empty<SnapshotEntry>();

        var result = SnapshotComparisonEngine.Compare(previous, current);

        result.Should().ContainSingle(x => x.ChangeType == ChangeType.Deleted);
    }

    [Fact]
    public void Compare_marks_same_path_with_new_size_as_size_changed()
    {
        var previous = new[] { SnapshotEntry.File("C:\\temp\\a.bin", 10, 10) };
        var current = new[] { SnapshotEntry.File("C:\\temp\\a.bin", 12, 12) };

        var result = SnapshotComparisonEngine.Compare(previous, current);

        result.Should().ContainSingle(
            x => x.ChangeType == ChangeType.SizeChanged && x.LogicalDelta == 2);
    }

    [Fact]
    public void Compare_marks_current_only_path_as_added()
    {
        var previous = Array.Empty<SnapshotEntry>();
        var current = new[] { SnapshotEntry.File("C:\\temp\\a.bin", 10, 10) };

        var result = SnapshotComparisonEngine.Compare(previous, current);

        result.Should().ContainSingle(x => x.ChangeType == ChangeType.Added);
    }

    [Fact]
    public void Compare_marks_same_path_with_same_sizes_as_unchanged()
    {
        var previous = new[] { SnapshotEntry.File("C:\\temp\\a.bin", 10, 10) };
        var current = new[] { SnapshotEntry.File("C:\\temp\\a.bin", 10, 10) };

        var result = SnapshotComparisonEngine.Compare(previous, current);

        result.Should().ContainSingle(x => x.ChangeType == ChangeType.Unchanged);
    }
}
