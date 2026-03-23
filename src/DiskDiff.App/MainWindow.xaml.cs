using DiskDiff.App.ViewModels;
using DiskDiff.Core.Models;
using System.Windows;
using System.Windows.Input;

namespace DiskDiff.App;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

    private void DirectoryTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is FolderTreeItemViewModel item)
        {
            ViewModel.SelectDirectory(item.Path);
        }
    }

    private void DetailsGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DetailsGrid.SelectedItem is DetailRowViewModel row && row.EntryType == EntryType.Directory)
        {
            ViewModel.SelectDirectory(row.Path);
        }
    }
}
