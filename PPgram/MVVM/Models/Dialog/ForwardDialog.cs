using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Message;
using PPgram.Shared;
using System.Collections.ObjectModel;
using System.Linq;

namespace PPgram.MVVM.Models.Dialog;
internal partial class ForwardDialog(MessageModel message, ObservableCollection<ChatModel> chats) : Dialog
{
    [ObservableProperty]
    public partial MessageModel Message { get; set; } = message;
    [ObservableProperty]
    public partial ObservableCollection<ChatModel> Chats { get; set; } = [.. chats];
    [ObservableProperty]
    public partial ChatModel? SelectedChat { get; set; }
    [ObservableProperty]
    public partial string SearchQuery {  get; set; }
    partial void OnSearchQueryChanged(string value)
    {
        if (!string.IsNullOrEmpty(value.Trim()))
        {
            SelectedChat = null;
            Chats = new(chats.Where(c => c.Profile.Name.Contains(SearchQuery) || c.Profile.Username.Contains(SearchQuery)));
        }
        else
        {
            SelectedChat = null;
            Chats = [.. chats];
        }
    }
    [RelayCommand]
    private void ClearSearch() => SearchQuery = string.Empty;
    [RelayCommand]
    private void Forward()
    {
        if (SelectedChat == null) return;
        Close();
        WeakReferenceMessenger.Default.Send(new Msg_ForwardMessage() {message = Message, to = SelectedChat});
    }
    [RelayCommand]
    private static void Close() => WeakReferenceMessenger.Default.Send(new Msg_CloseDialog());
}
