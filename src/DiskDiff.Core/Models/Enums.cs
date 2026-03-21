namespace DiskDiff.Core.Models;

public enum EntryType
{
    File,
    Directory,
}

public enum ChangeType
{
    Added,
    Deleted,
    SizeChanged,
    Unchanged,
}

public enum CompletionState
{
    Completed,
    Partial,
    Failed,
}
