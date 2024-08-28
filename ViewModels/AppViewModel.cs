using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Fontisso.NET.ViewModels;

public partial class AppViewModel : ViewModelBase
{
    [ObservableProperty]
    private string fileName;

    [ObservableProperty]
    private Bitmap fileIcon;

    [ObservableProperty]
    private bool hasFile;
}