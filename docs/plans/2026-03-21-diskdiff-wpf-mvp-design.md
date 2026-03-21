# DiskDiff WPF MVP Design

**Date:** 2026-03-21

## Goal

Build a Windows WPF desktop MVP that can:

- scan a selected drive root such as `C:\` using a recursive filesystem scan
- save each scan as a full snapshot in SQLite
- compare the newest snapshot with the previous snapshot for the same drive
- show directory-level and file-level size and delta information in a WPF shell

This MVP intentionally avoids the NTFS fast path and other advanced features so the team can first prove the end-to-end desktop workflow.

## Chosen Approach

Use a layered solution with separate `App`, `Core`, and `Infrastructure` projects.

- `DiskDiff.App` owns the WPF window, view models, commands, and UI state.
- `DiskDiff.Core` owns snapshot models, path normalization, diff rules, aggregation, and application services.
- `DiskDiff.Infrastructure` owns the recursive scan engine, SQLite persistence, and filesystem-specific behavior.

This structure costs a little more at bootstrap time, but it keeps the MVP extensible. The later NTFS scanner can replace the scan engine without forcing a redesign of the UI, persistence, or diff logic.

## MVP Boundaries

### In scope

- Windows desktop app with a single main window
- Drive selection for whole-volume scans
- Recursive scan of the selected drive root
- Full snapshot persistence in SQLite
- Compare `latest` versus `previous` snapshot for the same drive
- Diff categories:
  - `Added`
  - `Deleted`
  - `SizeChanged`
- Directory tree and detail table views
- Scan status, error count, snapshot status, and diff summary
- Partial-scan handling for inaccessible paths

### Out of scope

- NTFS MFT fast scan path
- Treemap visualization
- Automatic scheduled scans
- Explorer integration
- Arbitrary snapshot-pair comparison
- Rename or move detection
- Hashing or content-based comparisons
- File deletion or any destructive file actions
- Search and advanced UI filtering beyond the minimum diff-type filter

## Solution Structure

```text
DiskDiff.sln
Directory.Build.props
src/
  DiskDiff.App/
  DiskDiff.Core/
  DiskDiff.Infrastructure/
tests/
  DiskDiff.App.Tests/
  DiskDiff.Core.Tests/
  DiskDiff.Infrastructure.Tests/
docs/plans/
```

### Project responsibilities

`DiskDiff.App`
- WPF shell and layout
- view models and command binding
- loading and presenting comparison results

`DiskDiff.Core`
- domain models for snapshots, entries, errors, and comparison records
- path normalization rules
- diff engine and aggregation engine
- application services such as snapshot capture and latest comparison

`DiskDiff.Infrastructure`
- recursive filesystem scanner
- SQLite schema initialization and repository implementation
- file attribute and timestamp extraction

## Runtime Data Flow

1. The user launches the WPF app.
2. The main window shows the available drive letters and latest snapshot summary.
3. The user selects a drive such as `C:` and clicks `Scan Now`.
4. The app creates a `ScanRequest` for the drive root, for example `C:\`.
5. `SnapshotCaptureService` calls the recursive scan engine.
6. The scan engine returns `SnapshotMetadata`, `SnapshotEntry` rows, and any `ScanErrorRecord` values.
7. The snapshot repository stores the metadata, entries, and errors in SQLite.
8. `LatestComparisonService` loads the newest and previous snapshots for that drive.
9. The diff engine compares snapshot entries by normalized absolute path.
10. The aggregation engine rolls file-level values up into directory nodes.
11. The WPF view model updates the directory tree, details table, summary badges, and status bar.

## Domain Model

### Scan request and result

The scan engine should not write directly to SQLite. It returns a scan result that later services can persist and compare.

- `ScanRequest`
  - `DriveRoot`

- `ScanResult`
  - `Metadata`
  - `Entries`
  - `Errors`

### Snapshot metadata

`SnapshotMetadata`
- `SnapshotId`
- `DriveLetter`
- `RootPath`
- `FileSystemType`
- `ScanMode`
- `StartedAtUtc`
- `CompletedAtUtc`
- `ItemCount`
- `TotalLogicalBytes`
- `TotalAllocatedBytes`
- `CompletionState`
- `ErrorCount`

### Snapshot entries

`SnapshotEntry`
- `SnapshotId`
- `Path`
- `ParentPath`
- `Name`
- `EntryType`
- `LogicalBytes`
- `AllocatedBytes`
- `ModifiedAtUtc`
- `Attributes`

For the MVP, file `AllocatedBytes` may initially match `LogicalBytes`. Directory sizes are derived later through aggregation.

### Scan errors

`ScanErrorRecord`
- `SnapshotId`
- `Path`
- `Phase`
- `ErrorCode`
- `Message`

### Comparison results

`SnapshotComparisonRecord`
- `Path`
- `ParentPath`
- `Name`
- `EntryType`
- `ChangeType`
- `CurrentLogicalBytes`
- `PreviousLogicalBytes`
- `CurrentAllocatedBytes`
- `PreviousAllocatedBytes`
- `LogicalDelta`
- `AllocatedDelta`

`ChangeType`
- `Added`
- `Deleted`
- `SizeChanged`
- `Unchanged`

Comparison rules:

- present in current, missing in previous -> `Added`
- present in previous, missing in current -> `Deleted`
- same normalized path with different size -> `SizeChanged`
- same normalized path with the same size -> `Unchanged`

### Directory aggregation

`AggregatedNode`
- `Path`
- `ParentPath`
- `Name`
- `AllocatedBytes`
- `LogicalBytes`
- `AllocatedDelta`
- `LogicalDelta`
- `ChildCount`
- `ChangedDescendantCount`

Aggregation rules:

- file sizes roll up to every ancestor directory
- `Added` contributes positive delta
- `Deleted` contributes negative delta
- `SizeChanged` contributes the current-minus-previous delta
- a directory with changed descendants must expose a non-zero `ChangedDescendantCount` even if the directory entry itself did not change

## Path Rules

To keep snapshot comparison stable, all persisted paths must follow one normalization policy:

- store absolute Windows paths only
- uppercase the drive letter, for example `C:\Windows`
- remove trailing backslashes except for the drive root
- compare using case-insensitive Windows path semantics

The MVP will use normalized path strings as the identity for diffing. It will not attempt rename or move detection.

## Persistence Design

Use a local SQLite database under `%LocalAppData%\DiskDiff\`.

### Tables

`snapshots`
- `id`
- `drive_letter`
- `root_path`
- `file_system_type`
- `scan_mode`
- `started_at_utc`
- `completed_at_utc`
- `item_count`
- `total_logical_bytes`
- `total_allocated_bytes`
- `completion_state`
- `error_count`

`snapshot_entries`
- `snapshot_id`
- `path`
- `parent_path`
- `name`
- `entry_type`
- `logical_bytes`
- `allocated_bytes`
- `modified_at_utc`
- `attributes`

`scan_errors`
- `snapshot_id`
- `path`
- `phase`
- `error_code`
- `message`

### Required indexes

- `snapshot_entries(snapshot_id, path)`
- `snapshot_entries(snapshot_id, parent_path)`
- `snapshots(drive_letter, completed_at_utc desc)`

## Scanning Strategy

The MVP scanner uses streaming recursive enumeration instead of a full in-memory path preload.

Rules:

- enumerate directories and files incrementally
- include entries that are hidden or system if the OS enumeration returns them
- emit both directory and file entries
- do not compute directory sizes directly during scan
- detect reparse points and record them without descending into them

Avoiding reparse-point recursion in v1 reduces the risk of loops, duplicate traversal, and unintended cross-volume scans.

## Error Handling and Completion State

The app must be explicit when a snapshot is partial.

Completion states:

- `Completed`
- `Partial`
- `Failed`

Rules:

- a single inaccessible path must not abort the scan
- recoverable per-path failures are stored in `scan_errors`
- a scan that finishes with recoverable errors becomes `Partial`
- a scan that cannot successfully begin from the drive root becomes `Failed`

If a scan gathers substantial data and then hits protected directories, it still produces a usable `Partial` snapshot. If the root cannot be enumerated at all, the app should not pretend that a full snapshot exists.

## WPF UI Design

The MVP uses a single main window instead of multi-page navigation.

### Top command area

- drive selector
- `Scan Now` button
- latest snapshot timestamp
- status badge showing `Completed`, `Partial`, or `Failed`

### Left pane

Directory tree showing:

- directory name
- current aggregated size
- delta versus previous snapshot

### Right pane

Details table for the selected directory's direct children.

Columns:

- name
- type
- current size
- delta
- change type
- modified time

An optional minimum filter row may expose:

- `All`
- `Added`
- `Deleted`
- `SizeChanged`

### Bottom status area

- current scan phase or idle state
- scanned item count
- error count
- elapsed time

### Interaction rules

- after a completed load, the root directory is selected by default
- selecting a directory in the tree updates the detail table
- selecting a directory row in the table enters that directory
- selecting a file row highlights it without changing the current directory

## Application Service Boundaries

The WPF project should not directly coordinate scanning and persistence. The core layer should expose:

- `SnapshotCaptureService`
  - accepts a scan request
  - runs the scan
  - persists the resulting snapshot
  - returns persisted snapshot metadata

- `LatestComparisonService`
  - loads the latest and previous snapshots for a drive
  - computes comparison records
  - builds directory aggregation output for the UI

This keeps view models thin and allows the same services to support later entry points such as scheduled scans or quiet CLI modes.

## Testing Strategy

### `DiskDiff.Core.Tests`

Test pure domain logic:

- path normalization
- snapshot comparison rules
- folder aggregation
- delta handling for `Added`, `Deleted`, and `SizeChanged`

### `DiskDiff.Infrastructure.Tests`

Test external behavior:

- SQLite schema creation
- snapshot save and load round-trips
- latest-and-previous snapshot lookup
- recursive scan output for files and directories
- recoverable error capture
- reparse-point non-recursion

### `DiskDiff.App.Tests`

Test view model behavior rather than WPF visuals:

- scan command state transitions
- loading comparison results into tree and detail collections
- switching the selected directory updates visible rows
- `Completed`, `Partial`, and `Failed` status presentation in the view model

## Acceptance Criteria

The MVP is complete when it can:

- bootstrap a .NET 8 WPF solution and build successfully
- scan a selected drive such as `C:`
- persist the scan as a full snapshot in SQLite
- compare the latest snapshot with the previous snapshot for that drive
- show directory and direct-child detail results in the WPF shell
- record inaccessible paths without crashing
- distinguish `Completed`, `Partial`, and `Failed` snapshots clearly
