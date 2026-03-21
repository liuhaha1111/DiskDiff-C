# DiskDiff Snapshot Analyzer Design

**Date:** 2026-03-21

## Goal

Build a Windows desktop tool that helps identify why disk space on `C:` keeps shrinking by:

- scanning a volume quickly, with NTFS-first performance similar in spirit to WizTree
- saving full snapshots of the scanned volume
- comparing the newest snapshot with the previous snapshot
- showing `added`, `deleted`, and `size changed` items
- showing folder and file space usage through a tree, detail grid, and treemap

## Product Scope

### In scope for v1

- Windows desktop GUI
- Support any drive letter, default to `C:`
- Full-volume scans including system and hidden items
- Manual snapshot creation
- Daily automatic snapshot
- Snapshot comparison of `latest` versus `previous`
- Diff categories: `added`, `deleted`, `size changed`
- Folder and file size ranking
- Directory tree, detail grid, and treemap views
- Open file location in Explorer
- NTFS-optimized fast scan path
- Clear fallback path for non-NTFS or insufficient-privilege scenarios

### Explicit non-goals for v1

- Deleting files from the app
- Real-time file monitoring
- Comparing arbitrary snapshot pairs
- Detecting renames or moves as first-class events
- Cross-platform support
- High-performance support for FAT/exFAT/ReFS
- Cloud sync or multi-machine snapshot aggregation

## Primary User Workflow

1. Launch the app.
2. See the latest available snapshot summary for the default drive.
3. Run `Scan Now` or wait for the daily automatic snapshot.
4. The app stores the new snapshot and compares it against the previous snapshot for that drive.
5. The user drills down through:
   - folder tree with current size and delta
   - detail table with filters such as `All`, `Added`, `Deleted`, `Grown`
   - treemap to spot large space consumers quickly
6. When a suspicious item is found, the user opens its location in Explorer.

## Technical Direction

### Platform and stack

- `C#`
- `.NET 8`
- `WPF` for the desktop UI
- `CommunityToolkit.Mvvm` for view models and commands
- `Microsoft.Data.Sqlite` for local snapshot persistence
- `xUnit` plus `FluentAssertions` for tests
- Windows API / PInvoke for NTFS and shell integration

`WPF` is preferred over `WinUI` or `C++/Qt` for v1 because the product risk is dominated by large local datasets, virtualization, and Windows integration rather than modern cross-platform UI.

### High-level architecture

The app is split into five modules:

1. `Scan Engine`
   - default fast path for NTFS volumes
   - fallback filesystem API scan for unsupported volumes or degraded privilege scenarios

2. `Snapshot Store`
   - persists complete snapshots locally
   - stores scan metadata, entries, and scan errors

3. `Diff Engine`
   - compares newest snapshot to previous snapshot
   - produces `added`, `deleted`, and `size changed` records

4. `Aggregation Engine`
   - computes folder totals, subtree totals, and per-folder deltas
   - feeds the tree, grid, and treemap from a single aggregation model

5. `Desktop UI`
   - renders the workflow and exposes shell actions
   - never performs heavy computation on the UI thread

### Proposed solution structure

```text
DiskDiff.sln
src/
  DiskDiff.App/
  DiskDiff.Core/
  DiskDiff.Infrastructure/
  DiskDiff.Ntfs/
tests/
  DiskDiff.Core.Tests/
  DiskDiff.Infrastructure.Tests/
  DiskDiff.App.Tests/
docs/plans/
```

## Scan Engine

### Fast path

The primary scan path should read NTFS metadata directly instead of recursively enumerating the filesystem with standard directory APIs. The goal is to approach WizTree-style performance by working from NTFS metadata and reconstructing paths in memory.

The fast scanner must produce:

- stable item identity for the snapshot
- parent-child relationships
- full normalized path
- entry type: file or directory
- logical size
- allocated size
- timestamps
- file attributes
- scan errors when a record cannot be interpreted cleanly

### Fallback path

A fallback scanner is required for:

- non-NTFS volumes
- insufficient privilege for the fast path
- explicit fast-scanner initialization failure

Fallback mode may be slower. The UI must clearly label the scan as compatibility mode or partial mode.

## Snapshot Storage

v1 should use a local SQLite database stored under `%LocalAppData%\DiskDiff\`.

SQLite is the best v1 trade-off because it is:

- structured and queryable
- easy to index for millions of rows
- robust enough for local desktop use
- much simpler than inventing a custom binary snapshot format

### Snapshot metadata

Each snapshot stores:

- drive letter and root path
- volume serial number
- filesystem type
- scan mode: `ntfs-fast` or `fallback`
- start and end timestamps
- item counts
- total logical bytes
- total allocated bytes
- completion status
- error count

### Snapshot entry fields

Each file or directory entry stores at least:

- `snapshot_id`
- `path`
- `parent_path`
- `name`
- `entry_type`
- `file_reference`
- `parent_file_reference`
- `logical_bytes`
- `allocated_bytes`
- `created_at_utc`
- `modified_at_utc`
- `attributes`

`file_reference` is stored in v1 even though move/rename detection is out of scope. This keeps the door open for a later upgrade without changing the snapshot model.

### Error records

Store a separate error table for:

- path or record identifier
- error code
- short message
- phase

This supports transparent reporting of partial scans.

## Diff Rules

v1 compares the newest snapshot to the immediately previous snapshot for the same drive.

Rules:

- present in new snapshot, missing in old snapshot: `added`
- present in old snapshot, missing in new snapshot: `deleted`
- same normalized path, size changed: `size changed`
- same path, only timestamp changed: `unchanged`
- rename or move: represented as `deleted` plus `added`

The comparison should track both:

- `logical size delta`
- `allocated size delta`

The UI can default to allocated size for space analysis while still exposing logical size in details.

## Aggregation Model

The app needs a single aggregation pipeline that rolls file-level data up into folders.

Each folder node should expose:

- current total logical bytes
- current total allocated bytes
- delta versus previous snapshot
- child count
- changed descendant count

This enables:

- tree view summaries
- table sorting by size or delta
- treemap sizing
- filter chips such as `Added`, `Deleted`, `Grown`

## Desktop UI

The app should use one main window with synchronized panes.

### Top command area

- drive selector
- `Scan Now`
- `Create Snapshot`
- last snapshot timestamp
- auto-snapshot status
- scan mode badge

### Left pane

Folder tree showing:

- folder name
- allocated size
- delta since previous snapshot

### Upper-right pane

Detail grid for files and folders in the selected scope.

Columns should include:

- name
- type
- allocated size
- logical size
- delta
- modified time
- attributes
- change type

### Lower-right pane

Treemap sized by allocated bytes. Clicking a rectangle should synchronize the tree and detail grid.

### Bottom status area

- scan progress
- item counts
- duration
- completion state
- error count

## Automatic Snapshots

Daily automatic snapshots should be implemented through `Windows Task Scheduler`, not a resident background service.

When the user enables auto-snapshot:

1. the app registers a scheduled task
2. the task launches the app in a quiet command-line mode
3. the process scans the configured drive
4. the process saves the snapshot, updates latest-vs-previous diff data, logs result, and exits

Suggested default schedule: daily at `02:00`.

Benefits:

- no always-running background process
- simpler permissions story
- standard Windows operational model
- easier failure diagnostics

## Shell Integration

v1 should support:

- open containing folder
- reveal file in Explorer
- open file or folder properties if practical

v1 should not support delete or move actions.

## Error Handling and Trust Model

The app must avoid giving a false impression of completeness.

Requirements:

- individual entry failures do not abort the whole scan
- every scan records completion state
- the UI visibly distinguishes `complete`, `partial`, and `compatibility` scans
- automatic snapshot failures are logged and surfaced on next app launch
- users can inspect scan errors

## Performance Expectations

The v1 design targets:

- volumes with millions of entries
- responsive UI through asynchronous loading and virtualization
- background computation for scan, diff, and aggregation
- treemap rendering limited to the currently visualized scope

Snapshot retention should default to the most recent `30` days per drive.

## Acceptance Criteria

v1 is successful if it can:

- create manual snapshots for any chosen drive, defaulting to `C:`
- create daily automatic snapshots
- compare newest and previous snapshots for `added`, `deleted`, and `size changed`
- show space usage through tree, grid, and treemap
- open the selected item in Explorer
- survive scan errors without crashing
- clearly communicate degraded or partial scan conditions
- scan NTFS volumes substantially faster than naive recursive enumeration
