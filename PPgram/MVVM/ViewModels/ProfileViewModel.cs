using PPgram.App;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PPgram.MVVM.ViewModels;

internal partial class ProfileViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial ProfileState ProfileState {get; set; } = ProfileState.Instance;
    public ProfileViewModel()
    {

    }
}
