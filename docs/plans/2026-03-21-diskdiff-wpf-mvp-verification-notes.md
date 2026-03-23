# DiskDiff WPF MVP Verification Notes

**Recorded:** 2026-03-23

## Automated Verification

### Full test suite

Command:

```powershell
dotnet test DiskDiff.sln
```

Result:

- `DiskDiff.Core.Tests`: 21 passed, 0 failed
- `DiskDiff.Infrastructure.Tests`: 7 passed, 0 failed
- `DiskDiff.App.Tests`: 3 passed, 0 failed

### Release build

Command:

```powershell
dotnet build DiskDiff.sln -c Release
```

Result:

- Build succeeded
- 0 warnings
- 0 errors

## Startup Smoke Check

Command:

```powershell
$process = Start-Process -FilePath .\src\DiskDiff.App\bin\Release\net8.0-windows\DiskDiff.App.exe -PassThru
Start-Sleep -Seconds 5
Stop-Process -Id $process.Id
```

Observed result:

- The Release app process started successfully and remained alive for the smoke interval before termination.
- This verifies startup at the executable level.

## Manual GUI Smoke Scope

Full interactive click-through verification was not completed inside this terminal session.

Not verified manually here:

- selecting a drive in the live window
- clicking `Scan Now`
- inspecting persisted SQLite output after a real scan
- confirming comparison results after two live scans

The missing piece is interactive desktop validation, not build or automated test coverage.

## Known Limitations

- Uses the fallback recursive scanner rather than an NTFS fast path
- Compares only the latest snapshot against the previous snapshot for the same drive
- No rename or move detection
- No content hashing
- No treemap view
- No scheduled scan workflow
