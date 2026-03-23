using Microsoft.Data.Sqlite;

namespace DiskDiff.Infrastructure.Persistence;

public static class SnapshotDbInitializer
{
    public static async Task InitializeAsync(string databasePath, CancellationToken cancellationToken)
    {
        var directoryPath = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await using var connection = new SqliteConnection(BuildConnectionString(databasePath));
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS snapshots (
                id TEXT NOT NULL PRIMARY KEY,
                drive_letter TEXT NOT NULL,
                root_path TEXT NOT NULL,
                file_system_type TEXT NOT NULL,
                scan_mode TEXT NOT NULL,
                started_at_utc TEXT NOT NULL,
                completed_at_utc TEXT NOT NULL,
                item_count INTEGER NOT NULL,
                total_logical_bytes INTEGER NOT NULL,
                total_allocated_bytes INTEGER NOT NULL,
                completion_state TEXT NOT NULL,
                error_count INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS snapshot_entries (
                snapshot_id TEXT NOT NULL,
                path TEXT NOT NULL,
                parent_path TEXT NOT NULL,
                name TEXT NOT NULL,
                entry_type TEXT NOT NULL,
                logical_bytes INTEGER NOT NULL,
                allocated_bytes INTEGER NOT NULL,
                modified_at_utc TEXT NULL,
                attributes INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS scan_errors (
                snapshot_id TEXT NOT NULL,
                path TEXT NOT NULL,
                phase TEXT NOT NULL,
                error_code TEXT NOT NULL,
                message TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_snapshot_entries_snapshot_path
                ON snapshot_entries(snapshot_id, path);

            CREATE INDEX IF NOT EXISTS ix_snapshot_entries_snapshot_parent
                ON snapshot_entries(snapshot_id, parent_path);

            CREATE INDEX IF NOT EXISTS ix_snapshots_drive_completed
                ON snapshots(drive_letter, completed_at_utc DESC);
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal static string BuildConnectionString(string databasePath) => $"Data Source={databasePath};Pooling=False";
}

