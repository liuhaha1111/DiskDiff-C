namespace DiskDiff.Core.Services;

public interface ILatestComparisonService
{
    Task<LatestComparisonResult> GetLatestComparisonAsync(string driveLetter, CancellationToken cancellationToken);
}
