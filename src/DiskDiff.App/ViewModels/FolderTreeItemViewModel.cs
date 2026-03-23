using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace DiskDiff.App.ViewModels;

public sealed partial class FolderTreeItemViewModel : ObservableObject
{
    public FolderTreeItemViewModel(
        string path,
        string parentPath,
        string name,
        long logicalBytes,
        long logicalDelta,
        int childCount)
    {
        Path = path;
        ParentPath = parentPath;
        Name = name;
        LogicalBytes = logicalBytes;
        LogicalDelta = logicalDelta;
        ChildCount = childCount;
    }

    public string Path { get; }

    public string ParentPath { get; }

    public string Name { get; }

    public long LogicalBytes { get; }

    public long LogicalDelta { get; }

    public int ChildCount { get; }

    public ObservableCollection<FolderTreeItemViewModel> Children { get; } = new();

    [ObservableProperty]
    private bool isSelected;
}
