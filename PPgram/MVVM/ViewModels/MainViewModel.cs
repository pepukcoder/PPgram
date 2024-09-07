using Avalonia.Controls;
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
