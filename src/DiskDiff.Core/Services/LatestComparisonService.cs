using DiskDiff.Core.Abstractions;
using DiskDiff.Core.Aggregation;
using DiskDiff.Core.Comparison;
using DiskDiff.Core.Models;

namespace DiskDiff.Core.Services;

public sealed class LatestComparisonService : ILatestComparisonService
{
    private readonly ISnapshotRepository snapshotRepository;

    public LatestComparisonService(ISnapshotRepository snapshotRepository)
    {
        this.snapshotRepository = snapshotRepository ?? throw new ArgumentNullException(nameof(snapshotRepository));
    }

    public async Task<LatestComparisonResult> GetLatestComparisonAsync(string driveLetter, CancellationToken cancellationToken)
    {
        var (latest, previous) = await snapshotRepository.GetLatestAndPreviousAsync(
            NormalizeDriveLetter(driveLetter),
            cancellationToken);

        if (latest is null)
        {
            return LatestComparisonResult.Empty;
        }

        var currentEntries = await snapshotRepository.GetEntriesAsync(latest.SnapshotId, cancellationToken);

        if (previous is null)
        {
            return new LatestComparisonResult(
                latest,
                null,
                currentEntries,
                Array.Empty<SnapshotComparisonRecord>(),
                AggregationBuilder.Build(currentEntries, Array.Empty<SnapshotComparisonRecord>()));
        }

        var previousEntries = await snapshotRepository.GetEntriesAsync(previous.SnapshotId, cancellationToken);
        var comparisonRecords = SnapshotComparisonEngine.Compare(previousEntries, currentEntries);
        var aggregatedNodes = AggregationBuilder.Build(currentEntries, comparisonRecords);

        return new LatestComparisonResult(
            latest,
            previous,
            currentEntries,
            comparisonRecords,
            aggregatedNodes);
    }

    private static string NormalizeDriveLetter(string driveLetter)
    {
        if (string.IsNullOrWhiteSpace(driveLetter))
        {
            throw new ArgumentException("Drive letter cannot be null or whitespace.", nameof(driveLetter));
        }

        return driveLetter.Trim().TrimEnd(':').ToUpperInvariant();
    }
}
