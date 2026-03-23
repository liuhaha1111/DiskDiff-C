using DiskDiff.Core.Abstractions;
using DiskDiff.Core.Models;

namespace DiskDiff.Core.Services;

public sealed class SnapshotCaptureService : ISnapshotCaptureService
{
    private readonly IScanEngine scanEngine;
    private readonly ISnapshotRepository snapshotRepository;

    public SnapshotCaptureService(IScanEngine scanEngine, ISnapshotRepository snapshotRepository)
    {
        this.scanEngine = scanEngine ?? throw new ArgumentNullException(nameof(scanEngine));
        this.snapshotRepository = snapshotRepository ?? throw new ArgumentNullException(nameof(snapshotRepository));
    }

    public async Task<SnapshotMetadata> CaptureAsync(ScanRequest request, CancellationToken cancellationToken)
    {
        var scanResult = await scanEngine.ScanAsync(request, cancellationToken);
        await snapshotRepository.SaveSnapshotAsync(scanResult, cancellationToken);
        return scanResult.Metadata;
    }
}
