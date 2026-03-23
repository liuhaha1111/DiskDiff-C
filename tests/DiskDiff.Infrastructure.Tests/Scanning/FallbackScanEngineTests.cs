using DiskDiff.Core.Models;
using DiskDiff.Core.Pathing;
using DiskDiff.Infrastructure.Scanning;
using FluentAssertions;

namespace DiskDiff.Infrastructure.Tests.Scanning;

public sealed class FallbackScanEngineTests
{
    [Fact]
    public async Task ScanAsync_returns_file_and_directory_entries_for_a_root()
    {
        await using var fixture = new FileSystemFixture();
        var engine = new FallbackScanEngine();

        var result = await engine.ScanAsync(new ScanRequest(fixture.RootPath), CancellationToken.None);

        result.Metadata.CompletionState.Should().Be(CompletionState.Completed);
        result.Entries.Should().Contain(x => x.Path == WindowsPathNormalizer.Normalize(fixture.RootPath));
        result.Entries.Should().Contain(x => x.Path == WindowsPathNormalizer.Normalize(fixture.DirectoryPath));
        result.Entries.Should().Contain(x => x.Path == WindowsPathNormalizer.Normalize(fixture.FilePath));
    }

    [Fact]
    public async Task ScanAsync_marks_scan_as_partial_and_records_recoverable_errors()
    {
        var rootPath = "C:\\scan";
        var blockedPath = "C:\\scan\\blocked";
        var filePath = "C:\\scan\\visible.txt";
        var fileSystem = new FakeScanFileSystem(
            new Dictionary<string, IEnumerable<ScanFileSystemEntry>>(StringComparer.OrdinalIgnoreCase)
            {
                [rootPath] = new[]
                {
                    ScanFileSystemEntry.Directory(blockedPath),
                    ScanFileSystemEntry.File(filePath, 8),
                },
            },
            new Dictionary<string, Exception>(StringComparer.OrdinalIgnoreCase)
            {
                [blockedPath] = new UnauthorizedAccessException("Access denied."),
            });
        var engine = new FallbackScanEngine(fileSystem);

        var result = await engine.ScanAsync(new ScanRequest(rootPath), CancellationToken.None);

        result.Metadata.CompletionState.Should().Be(CompletionState.Partial);
        result.Errors.Should().ContainSingle(error => error.Path == WindowsPathNormalizer.Normalize(blockedPath));
        result.Entries.Should().Contain(entry => entry.Path == WindowsPathNormalizer.Normalize(filePath));
    }

    [Fact]
    public async Task ScanAsync_records_reparse_points_without_descending_into_them()
    {
        var rootPath = "C:\\scan";
        var reparsePath = "C:\\scan\\junction";
        var childBelowReparse = "C:\\scan\\junction\\child.txt";
        var fileSystem = new FakeScanFileSystem(
            new Dictionary<string, IEnumerable<ScanFileSystemEntry>>(StringComparer.OrdinalIgnoreCase)
            {
                [rootPath] = new[]
                {
                    ScanFileSystemEntry.Directory(reparsePath, FileAttributes.Directory | FileAttributes.ReparsePoint),
                },
                [reparsePath] = new[]
                {
                    ScanFileSystemEntry.File(childBelowReparse, 12),
                },
            });
        var engine = new FallbackScanEngine(fileSystem);

        var result = await engine.ScanAsync(new ScanRequest(rootPath), CancellationToken.None);

        result.Metadata.CompletionState.Should().Be(CompletionState.Completed);
        result.Entries.Should().Contain(entry => entry.Path == WindowsPathNormalizer.Normalize(reparsePath));
        result.Entries.Should().NotContain(entry => entry.Path == WindowsPathNormalizer.Normalize(childBelowReparse));
        fileSystem.EnumeratedPaths.Should().NotContain(path => path == WindowsPathNormalizer.Normalize(reparsePath));
    }

    [Fact]
    public async Task ScanAsync_marks_scan_as_failed_when_the_root_cannot_be_enumerated()
    {
        var rootPath = "C:\\scan";
        var fileSystem = new FakeScanFileSystem(
            new Dictionary<string, IEnumerable<ScanFileSystemEntry>>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, Exception>(StringComparer.OrdinalIgnoreCase)
            {
                [rootPath] = new DirectoryNotFoundException("Root missing."),
            });
        var engine = new FallbackScanEngine(fileSystem);

        var result = await engine.ScanAsync(new ScanRequest(rootPath), CancellationToken.None);

        result.Metadata.CompletionState.Should().Be(CompletionState.Failed);
        result.Metadata.ItemCount.Should().Be(0);
        result.Entries.Should().BeEmpty();
        result.Errors.Should().ContainSingle(error => error.Path == WindowsPathNormalizer.Normalize(rootPath));
    }

    private sealed class FakeScanFileSystem : IScanFileSystem
    {
        private readonly IReadOnlyDictionary<string, IEnumerable<ScanFileSystemEntry>> entriesByPath;
        private readonly IReadOnlyDictionary<string, Exception> exceptionsByPath;

        public FakeScanFileSystem(
            IReadOnlyDictionary<string, IEnumerable<ScanFileSystemEntry>> entriesByPath,
            IReadOnlyDictionary<string, Exception>? exceptionsByPath = null)
        {
            this.entriesByPath = entriesByPath;
            this.exceptionsByPath = exceptionsByPath
                ?? new Dictionary<string, Exception>(StringComparer.OrdinalIgnoreCase);
        }

        public List<string> EnumeratedPaths { get; } = new();

        public IEnumerable<ScanFileSystemEntry> EnumerateEntries(string directoryPath)
        {
            var normalizedPath = WindowsPathNormalizer.Normalize(directoryPath);
            EnumeratedPaths.Add(normalizedPath);

            if (exceptionsByPath.TryGetValue(normalizedPath, out var exception))
            {
                throw exception;
            }

            return entriesByPath.TryGetValue(normalizedPath, out var entries)
                ? entries
                : Array.Empty<ScanFileSystemEntry>();
        }

        public string GetFileSystemType(string rootPath) => "NTFS";
    }

    private sealed class FileSystemFixture : IAsyncDisposable
    {
        public FileSystemFixture()
        {
            RootPath = Path.Combine(Path.GetTempPath(), $"diskdiff-scan-{Guid.NewGuid():N}");
            Directory.CreateDirectory(RootPath);

            DirectoryPath = Path.Combine(RootPath, "folder");
            Directory.CreateDirectory(DirectoryPath);

            FilePath = Path.Combine(DirectoryPath, "data.bin");
            File.WriteAllBytes(FilePath, new byte[] { 1, 2, 3, 4 });
        }

        public string RootPath { get; }

        public string DirectoryPath { get; }

        public string FilePath { get; }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
