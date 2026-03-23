using DiskDiff.Core.Models;

namespace DiskDiff.Core.Services;

public interface ISnapshotCaptureService
{
    Task<SnapshotMetadata> CaptureAsync(ScanRequest request, CancellationToken cancellationToken);
}
