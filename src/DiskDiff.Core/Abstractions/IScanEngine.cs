using DiskDiff.Core.Models;

namespace DiskDiff.Core.Abstractions;

public interface IScanEngine
{
    Task<ScanResult> ScanAsync(ScanRequest request, CancellationToken cancellationToken);
}
