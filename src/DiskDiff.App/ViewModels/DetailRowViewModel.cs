using DiskDiff.Core.Models;

namespace DiskDiff.App.ViewModels;

public sealed record DetailRowViewModel(
    string Path,
    string ParentPath,
    string Name,
    EntryType EntryType,
    ChangeType ChangeType,
    long CurrentLogicalBytes,
    long LogicalDelta,
    DateTimeOffset? ModifiedAtUtc);
