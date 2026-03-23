using DiskDiff.Core.Models;
using DiskDiff.Infrastructure.Persistence;
using FluentAssertions;

namespace DiskDiff.Infrastructure.Tests.Persistence;

public sealed class SqliteSnapshotRepositoryTests
{
    [Fact]
    public async Task SaveSnapshotAsync_persists_metadata_entries_and_errors()
    {
        await using var harness = await SqliteHarness.CreateAsync();
        var repository = harness.CreateRepository();
        var scanResult = CreateScanResult(
            "C",
            startedAtUtc: new DateTimeOffset(2026, 3, 21, 1, 0, 0, TimeSpan.Zero),
            completedAtUtc: new DateTimeOffset(2026, 3, 21, 1, 5, 0, TimeSpan.Zero),
            errorCount: 1);

        await repository.SaveSnapshotAsync(scanResult, CancellationToken.None);

        var latest = await repository.GetLatestSnapshotAsync("C", CancellationToken.None);
        var entries = await repository.GetEntriesAsync(scanResult.Metadata.SnapshotId, CancellationToken.None);

        latest.Should().NotBeNull();
        latest!.ErrorCount.Should().Be(1);
        latest.ItemCount.Should().Be(2);
        entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLatestAndPreviousAsync_returns_snapshots_in_descending_time_order()
    {
        await using var harness = await SqliteHarness.CreateAsync();
        var repository = harness.CreateRepository();

        var earlier = CreateScanResult(
            "C",
            startedAtUtc: new DateTimeOffset(2026, 3, 21, 1, 0, 0, TimeSpan.Zero),
            completedAtUtc: new DateTimeOffset(2026, 3, 21, 1, 5, 0, TimeSpan.Zero));
        var later = CreateScanResult(
            "C",
            startedAtUtc: new DateTimeOffset(2026, 3, 21, 2, 0, 0, TimeSpan.Zero),
            completedAtUtc: new DateTimeOffset(2026, 3, 21, 2, 5, 0, TimeSpan.Zero));

        await repository.SaveSnapshotAsync(earlier, CancellationToken.None);
        await repository.SaveSnapshotAsync(later, CancellationToken.None);

        var pair = await repository.GetLatestAndPreviousAsync("C", CancellationToken.None);

        pair.Latest.Should().NotBeNull();
        pair.Previous.Should().NotBeNull();
        pair.Latest!.SnapshotId.Should().Be(later.Metadata.SnapshotId);
        pair.Previous!.SnapshotId.Should().Be(earlier.Metadata.SnapshotId);
    }

    private static ScanResult CreateScanResult(
        string driveLetter,
        DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc,
        int errorCount = 0)
    {
        var snapshotId = Guid.NewGuid();
        var metadata = new SnapshotMetadata(
            snapshotId,
            driveLetter,
            $"{driveLetter}:\\",
            "NTFS",
            "Fallback",
            startedAtUtc,
            completedAtUtc,
            ItemCount: 2,
            TotalLogicalBytes: 42,
            TotalAllocatedBytes: 42,
            CompletionState.Completed,
            errorCount);

        var entries = new[]
        {
            SnapshotEntry.Directory($"{driveLetter}:\\", snapshotId),
            SnapshotEntry.File($"{driveLetter}:\\temp\\a.bin", 42, 42, snapshotId),
        };

        var errors = errorCount == 0
            ? Array.Empty<ScanErrorRecord>()
            : new[]
            {
                new ScanErrorRecord(
                    snapshotId,
                    $"{driveLetter}:\\System Volume Information",
                    "Enumerate",
                    "UnauthorizedAccess",
                    "Access denied."),
            };

        return new ScanResult(metadata, entries, errors);
    }

    private sealed class SqliteHarness : IAsyncDisposable
    {
        private SqliteHarness(string databasePath)
        {
            DatabasePath = databasePath;
        }

        public string DatabasePath { get; }

        public static async Task<SqliteHarness> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
            await SnapshotDbInitializer.InitializeAsync(databasePath, CancellationToken.None);
            return new SqliteHarness(databasePath);
        }

        public SqliteSnapshotRepository CreateRepository() => new(DatabasePath);

        public ValueTask DisposeAsync()
        {
            if (File.Exists(DatabasePath))
            {
                File.Delete(DatabasePath);
            }

            return ValueTask.CompletedTask;
        }
    }
}
