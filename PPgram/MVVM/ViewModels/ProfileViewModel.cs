using PPgram.App;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PPgram.MVVM.ViewModels;

internal partial class ProfileViewModel : ViewModelBase
{
    [ObservableProperty]
    private ProfileState profile = ProfileState.Instance;
    public ProfileViewModel()
    {
        profile.Color = 4;
    }
}
