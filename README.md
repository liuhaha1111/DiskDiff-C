# DiskDiff WPF MVP

DiskDiff is a .NET 8 WPF desktop MVP for taking filesystem snapshots, persisting them to SQLite, and comparing the newest snapshot against the previous snapshot for the same drive.

## Solution Layout

```text
DiskDiff.sln
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

## What The MVP Does

- Scans a selected drive root such as `C:\`
- Persists snapshot metadata, entries, and recoverable scan errors into SQLite
- Compares the latest snapshot with the previous snapshot for the same drive
- Aggregates file-level changes up to directory nodes
- Displays directory navigation, direct-child detail rows, and scan status in a WPF shell

## Requirements

- Windows
- .NET 8 SDK

## Build And Test

```powershell
dotnet test DiskDiff.sln
dotnet build DiskDiff.sln -c Release
```

## Run The App

```powershell
dotnet run --project .\src\DiskDiff.App\DiskDiff.App.csproj
```

The app stores its SQLite database under `%LocalAppData%\DiskDiff\snapshots.db`.

## Current Limitations

- Uses the fallback recursive scanner, not an NTFS MFT fast path
- Compares `latest` versus `previous` snapshot only
- Does not detect renames or moves
- Does not hash file contents
- Does not include treemap visualization or scheduled scans

## Related Docs

- `docs/plans/2026-03-21-diskdiff-wpf-mvp-design.md`
- `docs/plans/2026-03-21-diskdiff-wpf-mvp-verification-notes.md`
