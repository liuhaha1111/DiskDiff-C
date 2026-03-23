using DiskDiff.App.ViewModels;
using DiskDiff.Core.Aggregation;
using DiskDiff.Core.Comparison;
using DiskDiff.Core.Models;
using DiskDiff.Core.Services;
using FluentAssertions;

namespace DiskDiff.App.Tests.ViewModels;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task ScanNowAsync_sets_status_and_loads_tree_rows()
    {
        var latest = CreateMetadata("C");
        var capture = new FakeSnapshotCaptureService(latest);
        var comparison = new FakeLatestComparisonService(CreateComparisonResult(latest));
        var viewModel = new MainWindowViewModel(capture, comparison, new[] { "C" });

        await viewModel.ScanNowAsync();

        viewModel.StatusText.Should().NotBeNullOrWhiteSpace();
        viewModel.DirectoryTreeItems.Should().NotBeEmpty();
        viewModel.DetailRows.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SelectDirectory_updates_detail_rows_to_direct_children()
    {
        var latest = CreateMetadata("C");
        var viewModel = new MainWindowViewModel(
            new FakeSnapshotCaptureService(latest),
            new FakeLatestComparisonService(CreateComparisonResult(latest)),
            new[] { "C" });

        await viewModel.ScanNowAsync();
        viewModel.SelectDirectory("C:\\Users");

        viewModel.DetailRows.Should().NotBeEmpty();
        viewModel.DetailRows.Should().OnlyContain(row => row.ParentPath == "C:\\Users");
        viewModel.DetailRows.Select(row => row.Name).Should().Contain("Administrator");
    }

    private static SnapshotMetadata CreateMetadata(string driveLetter) =>
        new(
            Guid.NewGuid(),
            driveLetter,
            $"{driveLetter}:\\",
            "NTFS",
            "Fallback",
            new DateTimeOffset(2026, 3, 22, 1, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 22, 1, 1, 0, TimeSpan.Zero),
            5,
            112,
            112,
            CompletionState.Completed,
            0);

    private static LatestComparisonResult CreateComparisonResult(SnapshotMetadata latest)
    {
        var entries = new SnapshotEntry[]
        {
            SnapshotEntry.Directory("C:\\", latest.SnapshotId),
            SnapshotEntry.Directory("C:\\Users", latest.SnapshotId),
            SnapshotEntry.Directory("C:\\Users\\Administrator", latest.SnapshotId),
            SnapshotEntry.File("C:\\pagefile.sys", 12, 12, latest.SnapshotId),
            SnapshotEntry.File("C:\\Users\\Administrator\\notes.txt", 100, 100, latest.SnapshotId),
        };

        var comparisons = new SnapshotComparisonRecord[]
        {
            new(
                "C:\\pagefile.sys",
                "C:\\",
                "pagefile.sys",
                EntryType.File,
                ChangeType.SizeChanged,
                12,
                10,
                12,
                10,
                2,
                2),
            new(
                "C:\\Users\\Administrator\\notes.txt",
                "C:\\Users\\Administrator",
                "notes.txt",
                EntryType.File,
                ChangeType.Added,
                100,
                0,
                100,
                0,
                100,
                100),
        };

        var aggregatedNodes = new Dictionary<string, AggregatedNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["C:\\"] = new("C:\\", string.Empty, "C:\\", 112, 112, 102, 102, 2, 2),
            ["C:\\Users"] = new("C:\\Users", "C:\\", "Users", 100, 100, 100, 100, 1, 1),
            ["C:\\Users\\Administrator"] = new("C:\\Users\\Administrator", "C:\\Users", "Administrator", 100, 100, 100, 100, 1, 1),
        };

        return new LatestComparisonResult(latest, null, entries, comparisons, aggregatedNodes);
    }

    private sealed class FakeSnapshotCaptureService : ISnapshotCaptureService
    {
        private readonly SnapshotMetadata metadata;

        public FakeSnapshotCaptureService(SnapshotMetadata metadata)
        {
            this.metadata = metadata;
        }

        public Task<SnapshotMetadata> CaptureAsync(ScanRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(metadata);
    }

    private sealed class FakeLatestComparisonService : ILatestComparisonService
    {
        private readonly LatestComparisonResult result;

        public FakeLatestComparisonService(LatestComparisonResult result)
        {
            this.result = result;
        }

        public Task<LatestComparisonResult> GetLatestComparisonAsync(string driveLetter, CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }
}
