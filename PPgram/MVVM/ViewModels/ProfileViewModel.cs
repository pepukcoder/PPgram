using CommunityToolkit.Mvvm.ComponentModel;
using PPgram.MVVM.Models.User;

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
