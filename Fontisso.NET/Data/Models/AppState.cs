using CommunityToolkit.Mvvm.ComponentModel;

namespace Fontisso.NET.Data.Models;

public interface IAppState
{
    TargetFileData FileData { get; }
    
    FontEntry? SelectedFont { get; set; }
}

public partial class AppState : ObservableObject, IAppState
{
    [ObservableProperty] private TargetFileData _fileData = TargetFileData.Default; 
    [ObservableProperty] private FontEntry? _selectedFont;
    
    public AppState()
    {
    }
}