using DiskDiff.Core.Models;

namespace DiskDiff.Core.Abstractions;

public interface ISnapshotRepository
{
    Task SaveSnapshotAsync(ScanResult scanResult, CancellationToken cancellationToken);

    Task<SnapshotMetadata?> GetLatestSnapshotAsync(string driveLetter, CancellationToken cancellationToken);

    Task<(SnapshotMetadata? Latest, SnapshotMetadata? Previous)> GetLatestAndPreviousAsync(
        string driveLetter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SnapshotEntry>> GetEntriesAsync(Guid snapshotId, CancellationToken cancellationToken);
}
