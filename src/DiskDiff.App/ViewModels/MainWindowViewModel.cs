using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskDiff.Core.Aggregation;
using DiskDiff.Core.Comparison;
using DiskDiff.Core.Models;
using DiskDiff.Core.Pathing;
using DiskDiff.Core.Services;
using System.Collections.ObjectModel;
using System.IO;

namespace DiskDiff.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly ISnapshotCaptureService snapshotCaptureService;
    private readonly ILatestComparisonService latestComparisonService;
    private LatestComparisonResult latestComparison = LatestComparisonResult.Empty;
    private readonly StringComparer pathComparer = StringComparer.OrdinalIgnoreCase;

    public MainWindowViewModel(
        ISnapshotCaptureService snapshotCaptureService,
        ILatestComparisonService latestComparisonService,
        IEnumerable<string>? availableDrives = null)
    {
        this.snapshotCaptureService = snapshotCaptureService ?? throw new ArgumentNullException(nameof(snapshotCaptureService));
        this.latestComparisonService = latestComparisonService ?? throw new ArgumentNullException(nameof(latestComparisonService));

        foreach (var drive in ResolveAvailableDrives(availableDrives))
        {
            AvailableDrives.Add(drive);
        }

        selectedDrive = AvailableDrives.FirstOrDefault() ?? "C";
        statusText = "Idle";
        latestSnapshotSummary = "No snapshots loaded.";
        ScanNowCommand = new AsyncRelayCommand(() => ScanNowAsync(), () => !IsBusy);
    }

    public ObservableCollection<string> AvailableDrives { get; } = new();

    public ObservableCollection<FolderTreeItemViewModel> DirectoryTreeItems { get; } = new();

    public ObservableCollection<DetailRowViewModel> DetailRows { get; } = new();

    public IAsyncRelayCommand ScanNowCommand { get; }

    [ObservableProperty]
    private string selectedDrive;

    [ObservableProperty]
    private string statusText;

    [ObservableProperty]
    private string latestSnapshotSummary;

    [ObservableProperty]
    private string selectedDirectoryPath = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private long scannedItemCount;

    [ObservableProperty]
    private int errorCount;

    [ObservableProperty]
    private string elapsedTimeText = "00:00:00";

    public async Task ScanNowAsync(CancellationToken cancellationToken = default)
    {
        var driveLetter = NormalizeDriveLetter(SelectedDrive);
        IsBusy = true;
        ScanNowCommand.NotifyCanExecuteChanged();
        StatusText = $"Scanning {driveLetter}:\\...";

        try
        {
            var rootPath = $"{driveLetter}:\\";
            var capturedMetadata = await snapshotCaptureService.CaptureAsync(
                new ScanRequest(rootPath),
                cancellationToken);

            var comparison = await latestComparisonService.GetLatestComparisonAsync(driveLetter, cancellationToken);
            LoadComparison(comparison, capturedMetadata);
        }
        finally
        {
            IsBusy = false;
            ScanNowCommand.NotifyCanExecuteChanged();
        }
    }

    public void SelectDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return;
        }

        var normalizedPath = WindowsPathNormalizer.Normalize(directoryPath);
        if (!CanSelectDirectory(normalizedPath))
        {
            return;
        }

        SelectedDirectoryPath = normalizedPath;
        UpdateTreeSelection(normalizedPath);
        RebuildDetailRows(normalizedPath);
    }

    private void LoadComparison(LatestComparisonResult comparison, SnapshotMetadata fallbackMetadata)
    {
        latestComparison = comparison.LatestSnapshot is null
            ? comparison with { LatestSnapshot = fallbackMetadata }
            : comparison;

        var latestSnapshot = latestComparison.LatestSnapshot ?? fallbackMetadata;
        LatestSnapshotSummary = BuildLatestSnapshotSummary(latestSnapshot, latestComparison.PreviousSnapshot);
        StatusText = BuildStatusText(latestSnapshot);
        ScannedItemCount = latestSnapshot.ItemCount;
        ErrorCount = latestSnapshot.ErrorCount;
        ElapsedTimeText = (latestSnapshot.CompletedAtUtc - latestSnapshot.StartedAtUtc).ToString(@"hh\:mm\:ss");

        RebuildDirectoryTree(latestComparison.AggregatedNodes);

        if (!string.IsNullOrEmpty(latestSnapshot.RootPath))
        {
            SelectDirectory(latestSnapshot.RootPath);
        }
        else
        {
            DetailRows.Clear();
        }
    }

    private void RebuildDirectoryTree(IReadOnlyDictionary<string, AggregatedNode> nodes)
    {
        DirectoryTreeItems.Clear();

        var itemsByPath = new Dictionary<string, FolderTreeItemViewModel>(pathComparer);
        foreach (var node in nodes.Values.OrderBy(node => node.Path.Length).ThenBy(node => node.Path, pathComparer))
        {
            itemsByPath[node.Path] = new FolderTreeItemViewModel(
                node.Path,
                node.ParentPath,
                node.Name,
                node.LogicalBytes,
                node.LogicalDelta,
                node.ChildCount);
        }

        foreach (var item in itemsByPath.Values.OrderBy(item => item.Path.Length).ThenBy(item => item.Path, pathComparer))
        {
            if (!string.IsNullOrEmpty(item.ParentPath) && itemsByPath.TryGetValue(item.ParentPath, out var parent))
            {
                parent.Children.Add(item);
            }
            else
            {
                DirectoryTreeItems.Add(item);
            }
        }
    }

    private void RebuildDetailRows(string directoryPath)
    {
        DetailRows.Clear();

        var comparisonByPath = latestComparison.ComparisonRecords.ToDictionary(
            record => record.Path,
            pathComparer);
        var directoryByPath = latestComparison.AggregatedNodes;

        var rows = latestComparison.CurrentEntries
            .Where(entry => pathComparer.Equals(entry.ParentPath, directoryPath))
            .Select(entry =>
            {
                if (entry.EntryType == EntryType.Directory
                    && directoryByPath.TryGetValue(entry.Path, out var directoryNode))
                {
                    comparisonByPath.TryGetValue(entry.Path, out var directoryComparison);
                    return new DetailRowViewModel(
                        directoryNode.Path,
                        directoryNode.ParentPath,
                        directoryNode.Name,
                        EntryType.Directory,
                        directoryComparison?.ChangeType ?? ChangeType.Unchanged,
                        directoryNode.LogicalBytes,
                        directoryNode.LogicalDelta,
                        entry.ModifiedAtUtc);
                }

                comparisonByPath.TryGetValue(entry.Path, out var comparisonRecord);
                return new DetailRowViewModel(
                    entry.Path,
                    entry.ParentPath,
                    entry.Name,
                    entry.EntryType,
                    comparisonRecord?.ChangeType ?? ChangeType.Unchanged,
                    comparisonRecord?.CurrentLogicalBytes ?? entry.LogicalBytes,
                    comparisonRecord?.LogicalDelta ?? 0,
                    entry.ModifiedAtUtc);
            })
            .OrderByDescending(row => row.EntryType == EntryType.Directory)
            .ThenBy(row => row.Name, pathComparer)
            .ToArray();

        foreach (var row in rows)
        {
            DetailRows.Add(row);
        }
    }

    private void UpdateTreeSelection(string selectedPath)
    {
        foreach (var item in EnumerateTreeItems(DirectoryTreeItems))
        {
            item.IsSelected = pathComparer.Equals(item.Path, selectedPath);
        }
    }

    private bool CanSelectDirectory(string normalizedPath)
    {
        return latestComparison.AggregatedNodes.ContainsKey(normalizedPath)
            || latestComparison.CurrentEntries.Any(entry =>
                entry.EntryType == EntryType.Directory
                && pathComparer.Equals(entry.Path, normalizedPath));
    }

    private static IEnumerable<FolderTreeItemViewModel> EnumerateTreeItems(IEnumerable<FolderTreeItemViewModel> rootItems)
    {
        foreach (var item in rootItems)
        {
            yield return item;

            foreach (var child in EnumerateTreeItems(item.Children))
            {
                yield return child;
            }
        }
    }

    private static IEnumerable<string> ResolveAvailableDrives(IEnumerable<string>? configuredDrives)
    {
        if (configuredDrives is not null)
        {
            return configuredDrives.Select(NormalizeDriveLetter).Distinct(StringComparer.OrdinalIgnoreCase);
        }

        var systemDrives = DriveInfo
            .GetDrives()
            .Where(drive => drive.IsReady)
            .Select(drive => NormalizeDriveLetter(drive.Name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return systemDrives.Length > 0 ? systemDrives : new[] { "C" };
    }

    private static string BuildLatestSnapshotSummary(SnapshotMetadata latest, SnapshotMetadata? previous)
    {
        var comparisonText = previous is null
            ? "No previous snapshot."
            : $"Compared with {previous.CompletedAtUtc:yyyy-MM-dd HH:mm}.";
        return $"Latest snapshot: {latest.CompletedAtUtc:yyyy-MM-dd HH:mm} ({latest.CompletionState}). {comparisonText}";
    }

    private static string BuildStatusText(SnapshotMetadata latest) =>
        latest.CompletionState switch
        {
            CompletionState.Completed => "Scan completed.",
            CompletionState.Partial => "Scan completed with recoverable errors.",
            CompletionState.Failed => "Scan failed.",
            _ => "Idle",
        };

    private static string NormalizeDriveLetter(string driveValue)
    {
        if (string.IsNullOrWhiteSpace(driveValue))
        {
            throw new ArgumentException("Drive value cannot be null or whitespace.", nameof(driveValue));
        }

        var trimmed = driveValue.Trim().TrimEnd('\\').TrimEnd(':');
        return trimmed.Length == 0 ? "C" : trimmed[..1].ToUpperInvariant();
    }
}


