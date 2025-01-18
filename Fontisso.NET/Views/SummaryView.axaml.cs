using Avalonia.Controls;
using Fontisso.NET.ViewModels;

namespace Fontisso.NET.Views;

public partial class SummaryView : UserControl
{
    public SummaryView()
    {
        InitializeComponent();
    }
    
    private async void PreviewContainer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is SummaryViewModel viewModel)
        {
            await viewModel.UpdatePreviewWidth(e.NewSize.Width);
        }
    }
}