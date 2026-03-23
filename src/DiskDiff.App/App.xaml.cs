using DiskDiff.App.ViewModels;
using DiskDiff.Core.Services;
using DiskDiff.Infrastructure.Persistence;
using DiskDiff.Infrastructure.Scanning;
using System.IO;
using System.Windows;

namespace DiskDiff.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DiskDiff",
            "snapshots.db");

        var repository = new SqliteSnapshotRepository(databasePath);
        var scanEngine = new FallbackScanEngine();
        var captureService = new SnapshotCaptureService(scanEngine, repository);
        var comparisonService = new LatestComparisonService(repository);
        var viewModel = new MainWindowViewModel(captureService, comparisonService);

        MainWindow = new MainWindow(viewModel);
        MainWindow.Show();
    }
}

