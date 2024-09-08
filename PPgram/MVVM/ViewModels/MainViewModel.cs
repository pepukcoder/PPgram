using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using PPgram.MVVM.ViewModels;

namespace PPgram.MVVM.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private UserControl _currentPage;


    public MainViewModel() 
    {

    }
}
