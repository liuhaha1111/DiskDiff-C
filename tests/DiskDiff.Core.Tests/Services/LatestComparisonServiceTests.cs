using DiskDiff.Core.Abstractions;
using DiskDiff.Core.Models;
using DiskDiff.Core.Services;
using FluentAssertions;

namespace DiskDiff.Core.Tests.Services;

public sealed class LatestComparisonServiceTests
{
    [Fact]
    public async Task GetLatestComparisonAsync_returns_empty_diff_when_no_previous_snapshot_exists()
    {
        var latest = CreateMetadata("C", completedAtUtc: new DateTimeOffset(2026, 3, 22, 2, 0, 0, TimeSpan.Zero));
        var repository = new FakeSnapshotRepository
        {
            Latest = latest,
            EntriesBySnapshotId =
            {
                [latest.SnapshotId] = new SnapshotEntry[]
                {
                    SnapshotEntry.Directory("C:\\", latest.SnapshotId),
                    SnapshotEntry.File("C:\\data.bin", 10, 10, latest.SnapshotId),
                },
            },
        };
        var service = new LatestComparisonService(repository);

        var result = await service.GetLatestComparisonAsync("C", CancellationToken.None);

        result.LatestSnapshot.Should().Be(latest);
        result.PreviousSnapshot.Should().BeNull();
        result.ComparisonRecords.Should().BeEmpty();
        result.AggregatedNodes.Should().ContainKey("C:\\");
    }

    [Fact]
    public async Task GetLatestComparisonAsync_builds_diffs_and_aggregation_from_latest_and_previous_snapshots()
    {
        var previous = CreateMetadata("C", completedAtUtc: new DateTimeOffset(2026, 3, 22, 1, 0, 0, TimeSpan.Zero));
        var latest = CreateMetadata("C", completedAtUtc: new DateTimeOffset(2026, 3, 22, 2, 0, 0, TimeSpan.Zero));
        var repository = new FakeSnapshotRepository
        {
            Latest = latest,
            Previous = previous,
            EntriesBySnapshotId =
            {
                [previous.SnapshotId] = new SnapshotEntry[]
                {
                    SnapshotEntry.Directory("C:\\", previous.SnapshotId),
                    SnapshotEntry.File("C:\\data.bin", 10, 10, previous.SnapshotId),
                },
                [latest.SnapshotId] = new SnapshotEntry[]
                {
                    SnapshotEntry.Directory("C:\\", latest.SnapshotId),
                    SnapshotEntry.File("C:\\data.bin", 12, 12, latest.SnapshotId),
                },
            },
        };
        var service = new LatestComparisonService(repository);

        var result = await service.GetLatestComparisonAsync("C", CancellationToken.None);

        result.ComparisonRecords.Should().ContainSingle(
            record => record.Path == "C:\\data.bin"
                && record.LogicalDelta == 2
                && record.ChangeType == ChangeType.SizeChanged);
        result.AggregatedNodes["C:\\"].LogicalDelta.Should().Be(2);
    }

    [Fact]
    public async Task GetLatestComparisonAsync_returns_empty_result_when_no_snapshots_exist()
    {
        var service = new LatestComparisonService(new FakeSnapshotRepository());

        var result = await service.GetLatestComparisonAsync("C", CancellationToken.None);

        result.LatestSnapshot.Should().BeNull();
        result.ComparisonRecords.Should().BeEmpty();
        result.AggregatedNodes.Should().BeEmpty();
    }

    private static SnapshotMetadata CreateMetadata(string driveLetter, DateTimeOffset completedAtUtc) =>
        new(
            Guid.NewGuid(),
            driveLetter,
            $"{driveLetter}:\\",
            "NTFS",
            "Fallback",
            completedAtUtc.AddMinutes(-1),
            completedAtUtc,
            2,
            12,
            12,
            CompletionState.Completed,
            0);

    private sealed class FakeSnapshotRepository : ISnapshotRepository
    {
        public SnapshotMetadata? Latest { get; init; }

        public SnapshotMetadata? Previous { get; init; }

        public Dictionary<Guid, IReadOnlyList<SnapshotEntry>> EntriesBySnapshotId { get; } = new();

        public Task SaveSnapshotAsync(ScanResult scanResult, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<SnapshotMetadata?> GetLatestSnapshotAsync(string driveLetter, CancellationToken cancellationToken) =>
            Task.FromResult(Latest);

        public Task<(SnapshotMetadata? Latest, SnapshotMetadata? Previous)> GetLatestAndPreviousAsync(
            string driveLetter,
            CancellationToken cancellationToken) =>
            Task.FromResult((Latest, Previous));

        public Task<IReadOnlyList<SnapshotEntry>> GetEntriesAsync(Guid snapshotId, CancellationToken cancellationToken) =>
            Task.FromResult(
                EntriesBySnapshotId.TryGetValue(snapshotId, out var entries)
                    ? entries
                    : (IReadOnlyList<SnapshotEntry>)Array.Empty<SnapshotEntry>());
    }
}
