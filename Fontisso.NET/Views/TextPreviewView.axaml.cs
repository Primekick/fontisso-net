using Avalonia.Controls;
using Fontisso.NET.ViewModels;

namespace Fontisso.NET.Views;

public partial class TextPreviewView : UserControl
{
    public TextPreviewView()
    {
        InitializeComponent();
    }
    
    private async void PreviewContainer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is TextPreviewViewModel viewModel)
        {
            await viewModel.UpdatePreviewWidth(e.NewSize.Width);
        }
    }
}