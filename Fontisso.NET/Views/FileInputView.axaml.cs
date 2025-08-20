using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Fontisso.NET.ViewModels;

namespace Fontisso.NET.Views;

public partial class FileInputView : UserControl
{
    public FileInputView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, Drop);
        AddHandler(DragDrop.DragOverEvent, DragOver);
    }

    private void DragOver(object? sender, DragEventArgs dragEvent)
    {
        ArgumentNullException.ThrowIfNull(sender);
        dragEvent.DragEffects &= DragDropEffects.Copy | DragDropEffects.Link;

        if (!dragEvent.Data.Contains(DataFormats.Files))
        {
            dragEvent.DragEffects = DragDropEffects.None;
        }
    }

    private void Drop(object? sender, DragEventArgs dragEvent)
    {
        if (!dragEvent.Data.Contains(DataFormats.Files))
        {
            return;
        }
        
        var files = dragEvent.Data.GetFiles();
        if (DataContext is FileInputViewModel viewModel && files is not null)
        {
            viewModel.HandleDroppedFileAsync(files.Select(file => file.Path.LocalPath).ToArray());
        }
    }
}