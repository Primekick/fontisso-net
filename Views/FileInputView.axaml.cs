using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
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

    private void DragOver(object sender, DragEventArgs dragEvent)
    {
        dragEvent.DragEffects &= DragDropEffects.Copy | DragDropEffects.Link;

        if (!dragEvent.Data.Contains(DataFormats.Files))
        {
            dragEvent.DragEffects = DragDropEffects.None;
        }
    }

    private async Task Drop(object sender, DragEventArgs dragEvent)
    {
        if (!dragEvent.Data.Contains(DataFormats.Files))
        {
            return;
        }
        
        var files = dragEvent.Data.GetFileNames();
        if (DataContext is FileInputViewModel viewModel)
        {
            await viewModel.HandleDroppedFileAsync(files.ToArray());
        }
    }
}