using DiskDiff.Core.Abstractions;
using DiskDiff.Core.Models;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace DiskDiff.Infrastructure.Persistence;

public sealed class SqliteSnapshotRepository : ISnapshotRepository
{
    private readonly string databasePath;

    public SqliteSnapshotRepository(string databasePath)
    {
        this.databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
    }

    public async Task SaveSnapshotAsync(ScanResult scanResult, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scanResult);

        await SnapshotDbInitializer.InitializeAsync(databasePath, cancellationToken);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await InsertSnapshotAsync(connection, transaction, scanResult.Metadata, cancellationToken);

        foreach (var entry in scanResult.Entries)
        {
            await InsertEntryAsync(connection, transaction, scanResult.Metadata.SnapshotId, entry, cancellationToken);
        }

        foreach (var error in scanResult.Errors)
        {
            await InsertErrorAsync(connection, transaction, scanResult.Metadata.SnapshotId, error, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<SnapshotMetadata?> GetLatestSnapshotAsync(string driveLetter, CancellationToken cancellationToken)
    {
        var snapshots = await GetTopSnapshotsAsync(NormalizeDriveLetter(driveLetter), 1, cancellationToken);
        return snapshots.SingleOrDefault();
    }

    public async Task<(SnapshotMetadata? Latest, SnapshotMetadata? Previous)> GetLatestAndPreviousAsync(
        string driveLetter,
        CancellationToken cancellationToken)
    {
        var snapshots = await GetTopSnapshotsAsync(NormalizeDriveLetter(driveLetter), 2, cancellationToken);
        return (
            snapshots.ElementAtOrDefault(0),
            snapshots.ElementAtOrDefault(1));
    }

    public async Task<IReadOnlyList<SnapshotEntry>> GetEntriesAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        await SnapshotDbInitializer.InitializeAsync(databasePath, cancellationToken);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT snapshot_id,
                   path,
                   parent_path,
                   name,
                   entry_type,
                   logical_bytes,
                   allocated_bytes,
                   modified_at_utc,
                   attributes
            FROM snapshot_entries
            WHERE snapshot_id = $snapshotId
            ORDER BY path;
            """;
        command.Parameters.AddWithValue("$snapshotId", snapshotId.ToString());

        var entries = new List<SnapshotEntry>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new SnapshotEntry(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                Enum.Parse<EntryType>(reader.GetString(4), ignoreCase: true),
                reader.GetInt64(5),
                reader.GetInt64(6),
                reader.IsDBNull(7) ? null : DateTimeOffset.Parse(reader.GetString(7)),
                (FileAttributes)reader.GetInt64(8)));
        }

        return entries;
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(SnapshotDbInitializer.BuildConnectionString(databasePath));
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private async Task<IReadOnlyList<SnapshotMetadata>> GetTopSnapshotsAsync(
        string driveLetter,
        int takeCount,
        CancellationToken cancellationToken)
    {
        await SnapshotDbInitializer.InitializeAsync(databasePath, cancellationToken);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id,
                   drive_letter,
                   root_path,
                   file_system_type,
                   scan_mode,
                   started_at_utc,
                   completed_at_utc,
                   item_count,
                   total_logical_bytes,
                   total_allocated_bytes,
                   completion_state,
                   error_count
            FROM snapshots
            WHERE drive_letter = $driveLetter
            ORDER BY completed_at_utc DESC
            LIMIT $takeCount;
            """;
        command.Parameters.AddWithValue("$driveLetter", driveLetter);
        command.Parameters.AddWithValue("$takeCount", takeCount);

        var snapshots = new List<SnapshotMetadata>(takeCount);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            snapshots.Add(new SnapshotMetadata(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                DateTimeOffset.Parse(reader.GetString(5)),
                DateTimeOffset.Parse(reader.GetString(6)),
                reader.GetInt64(7),
                reader.GetInt64(8),
                reader.GetInt64(9),
                Enum.Parse<CompletionState>(reader.GetString(10), ignoreCase: true),
                reader.GetInt32(11)));
        }

        return snapshots;
    }

    private static async Task InsertSnapshotAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        SnapshotMetadata metadata,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction;
        command.CommandText =
            """
            INSERT INTO snapshots (
                id,
                drive_letter,
                root_path,
                file_system_type,
                scan_mode,
                started_at_utc,
                completed_at_utc,
                item_count,
                total_logical_bytes,
                total_allocated_bytes,
                completion_state,
                error_count)
            VALUES (
                $id,
                $driveLetter,
                $rootPath,
                $fileSystemType,
                $scanMode,
                $startedAtUtc,
                $completedAtUtc,
                $itemCount,
                $totalLogicalBytes,
                $totalAllocatedBytes,
                $completionState,
                $errorCount);
            """;
        command.Parameters.AddWithValue("$id", metadata.SnapshotId.ToString());
        command.Parameters.AddWithValue("$driveLetter", NormalizeDriveLetter(metadata.DriveLetter));
        command.Parameters.AddWithValue("$rootPath", metadata.RootPath);
        command.Parameters.AddWithValue("$fileSystemType", metadata.FileSystemType);
        command.Parameters.AddWithValue("$scanMode", metadata.ScanMode);
        command.Parameters.AddWithValue("$startedAtUtc", metadata.StartedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$completedAtUtc", metadata.CompletedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$itemCount", metadata.ItemCount);
        command.Parameters.AddWithValue("$totalLogicalBytes", metadata.TotalLogicalBytes);
        command.Parameters.AddWithValue("$totalAllocatedBytes", metadata.TotalAllocatedBytes);
        command.Parameters.AddWithValue("$completionState", metadata.CompletionState.ToString());
        command.Parameters.AddWithValue("$errorCount", metadata.ErrorCount);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertEntryAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        Guid snapshotId,
        SnapshotEntry entry,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction;
        command.CommandText =
            """
            INSERT INTO snapshot_entries (
                snapshot_id,
                path,
                parent_path,
                name,
                entry_type,
                logical_bytes,
                allocated_bytes,
                modified_at_utc,
                attributes)
            VALUES (
                $snapshotId,
                $path,
                $parentPath,
                $name,
                $entryType,
                $logicalBytes,
                $allocatedBytes,
                $modifiedAtUtc,
                $attributes);
            """;
        command.Parameters.AddWithValue("$snapshotId", snapshotId.ToString());
        command.Parameters.AddWithValue("$path", entry.Path);
        command.Parameters.AddWithValue("$parentPath", entry.ParentPath);
        command.Parameters.AddWithValue("$name", entry.Name);
        command.Parameters.AddWithValue("$entryType", entry.EntryType.ToString());
        command.Parameters.AddWithValue("$logicalBytes", entry.LogicalBytes);
        command.Parameters.AddWithValue("$allocatedBytes", entry.AllocatedBytes);
        command.Parameters.AddWithValue(
            "$modifiedAtUtc",
            entry.ModifiedAtUtc is null ? DBNull.Value : entry.ModifiedAtUtc.Value.ToString("O"));
        command.Parameters.AddWithValue("$attributes", (long)entry.Attributes);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertErrorAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        Guid snapshotId,
        ScanErrorRecord error,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction;
        command.CommandText =
            """
            INSERT INTO scan_errors (
                snapshot_id,
                path,
                phase,
                error_code,
                message)
            VALUES (
                $snapshotId,
                $path,
                $phase,
                $errorCode,
                $message);
            """;
        command.Parameters.AddWithValue("$snapshotId", snapshotId.ToString());
        command.Parameters.AddWithValue("$path", error.Path);
        command.Parameters.AddWithValue("$phase", error.Phase);
        command.Parameters.AddWithValue("$errorCode", error.ErrorCode);
        command.Parameters.AddWithValue("$message", error.Message);
        await command.ExecuteNonQueryAsync(cancellationToken);
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




