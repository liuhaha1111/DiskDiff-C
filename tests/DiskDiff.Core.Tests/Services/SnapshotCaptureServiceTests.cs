using DiskDiff.Core.Abstractions;
using DiskDiff.Core.Models;
using DiskDiff.Core.Services;
using FluentAssertions;

namespace DiskDiff.Core.Tests.Services;

public sealed class SnapshotCaptureServiceTests
{
    [Fact]
    public async Task CaptureAsync_runs_the_scan_and_persists_the_snapshot()
    {
        var scanResult = CreateScanResult("C", 12);
        var scanner = new FakeScanEngine(scanResult);
        var repository = new FakeSnapshotRepository();
        var service = new SnapshotCaptureService(scanner, repository);

        var metadata = await service.CaptureAsync(new ScanRequest("C:\\"), CancellationToken.None);

        metadata.DriveLetter.Should().Be("C");
        repository.SavedResults.Should().ContainSingle().Which.Should().BeSameAs(scanResult);
        scanner.Requests.Should().ContainSingle().Which.DriveRoot.Should().Be("C:\\");
    }

    private static ScanResult CreateScanResult(string driveLetter, long size)
    {
        var snapshotId = Guid.NewGuid();
        var metadata = new SnapshotMetadata(
            snapshotId,
            driveLetter,
            $"{driveLetter}:\\",
            "NTFS",
            "Fallback",
            new DateTimeOffset(2026, 3, 22, 1, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 22, 1, 1, 0, TimeSpan.Zero),
            2,
            size,
            size,
            CompletionState.Completed,
            0);

        return new ScanResult(
            metadata,
            new SnapshotEntry[]
            {
                SnapshotEntry.Directory($"{driveLetter}:\\", snapshotId),
                SnapshotEntry.File($"{driveLetter}:\\data.bin", size, size, snapshotId),
            },
            Array.Empty<ScanErrorRecord>());
    }

    private sealed class FakeScanEngine : IScanEngine
    {
        private readonly ScanResult result;

        public FakeScanEngine(ScanResult result)
        {
            this.result = result;
        }

        public List<ScanRequest> Requests { get; } = new();

        public Task<ScanResult> ScanAsync(ScanRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(result);
        }
    }

    private sealed class FakeSnapshotRepository : ISnapshotRepository
    {
        public List<ScanResult> SavedResults { get; } = new();

        public Task SaveSnapshotAsync(ScanResult scanResult, CancellationToken cancellationToken)
        {
            SavedResults.Add(scanResult);
            return Task.CompletedTask;
        }

        public Task<SnapshotMetadata?> GetLatestSnapshotAsync(string driveLetter, CancellationToken cancellationToken) =>
            Task.FromResult<SnapshotMetadata?>(null);

        public Task<(SnapshotMetadata? Latest, SnapshotMetadata? Previous)> GetLatestAndPreviousAsync(
            string driveLetter,
            CancellationToken cancellationToken) =>
            Task.FromResult<(SnapshotMetadata?, SnapshotMetadata?)>((null, null));

        public Task<IReadOnlyList<SnapshotEntry>> GetEntriesAsync(Guid snapshotId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SnapshotEntry>>(Array.Empty<SnapshotEntry>());
    }
}
