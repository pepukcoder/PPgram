using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.App;
using PPgram.Helpers;
using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Dialog;
using PPgram.MVVM.Models.Message;
using PPgram.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PPgram.MVVM.ViewModels;

partial class ChatViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<ChatModel> chats = [];
    [ObservableProperty]
    private ObservableCollection<ChatModel> searchResults = [];
    [ObservableProperty]
    private ObservableCollection<FolderModel> folders =
    [
        new FolderModel("Archive", is_archive: true),
        new FolderModel("All", is_all: true),
        new FolderModel("Personal", is_personal: true)
    ];
    [ObservableProperty]
    private ChatModel? selectedChat;
    [ObservableProperty]
    private ChatModel? selectedSearch;
    [ObservableProperty]
    private FolderModel? selectedFolder;
    [ObservableProperty]
    private ProfileState profileState = ProfileState.Instance;
    [ObservableProperty]
    private string searchInput = string.Empty;
    [ObservableProperty]
    private bool platesVisible;

    private readonly DispatcherTimer timer;
    public ChatViewModel()
    {
        PlatesVisible = false;
        // search request delay timer
        timer = new() { Interval = TimeSpan.FromMilliseconds(200) };
        timer.Tick += SearchChat;
    }
    partial void OnSearchInputChanged(string value)
    {
        // stop timer if still editing
        timer.Stop();
        if (!String.IsNullOrEmpty(value.Trim()))
        {
            // restart delay if done editing
            timer.Start();
        }
    }
    partial void OnSelectedSearchChanged(ChatModel? oldValue, ChatModel? newValue)
    {
        if (String.IsNullOrEmpty(SearchInput)) return;
        if (newValue == null) return;
        if (Chats.Any(c => c.Id == newValue?.Id))
        {
            SelectedChat = Chats.FirstOrDefault(c => c.Id == newValue?.Id);
            ClearSearch();
        }
        else
        {
            SelectedChat = newValue;
        }
    }
    partial void OnSelectedChatChanged(ChatModel? oldValue, ChatModel? newValue)
    {
        if (newValue != null) newValue.UnreadCount = 0;
    }
    private void SearchChat(object? sender, EventArgs e)
    {
        // stop timer to prevent request spam
        timer.Stop();
        WeakReferenceMessenger.Default.Send(new Msg_SearchUsers { query = SearchInput.Trim() });
    }
    private bool TryFindChat(int id, out ChatModel chat)
    {
        ChatModel? chat_or_null = Chats.FirstOrDefault(c => c.Id == id);
        chat = chat_or_null ?? default!;
        return chat_or_null != null;
    }
    public void UpdateSearch(ObservableCollection<ChatModel> resultList)
    {
        SearchResults = resultList;
        SelectedSearch = null;
    }
    public void AddChatIfNotExists(ChatModel chat)
    {
        if (!TryFindChat(chat.Id, out var c))
        {
            Chats.Add(chat);
            chat.Searched = false;
            // setting null to actually update ui property and show selection
            SelectedChat = null;
            SelectedChat = chat;
            ClearSearch();
        }
    }
    public void AddMessage(MessageModel message)
    {
        if (TryFindChat(message.Chat, out var chat))
        {
            chat.AddMessage(message);
            if (SelectedChat != chat) chat.UnreadCount++;
        }
    }
    public void EditMessage(MessageModel message)
    {
        if (TryFindChat(message.Chat, out var chat)) chat.EditMessage(message);
    }
    public void DeleteMessage(int chat_id, int message_id)
    {
        if (TryFindChat(chat_id, out var chat)) chat.DeleteMessage(message_id);
    }
    public void LoadMessages(int chat_id, List<MessageModel> messages)
    {
        if (TryFindChat(chat_id, out var chat)) chat.LoadMessages(messages);
    }
    public void ChangeChatStatus(int chat_id, ChatStatus status)
    {
        if (TryFindChat(chat_id, out var chat))
        {
            chat.Status = status;
            // TODO: for group pass a typing user name fetched from id
        }
    }
    public void ChangeMessageStatus(int chat_id, int message_id, MessageStatus status)
    {
        if (TryFindChat(chat_id, out var chat)) chat.ChangeMessageStatus(message_id, status);
    }
    [RelayCommand]
    private void OpenNewGroupDialog()
    {
        WeakReferenceMessenger.Default.Send(new Msg_ShowDialog { dialog = new NewGroupDialog() });
    }
    [RelayCommand]
    private void ClearSearch() => SearchInput = string.Empty;
    [RelayCommand]
    private void CloseChat() => SelectedChat = null;
    [RelayCommand]
    private void Logout() => WeakReferenceMessenger.Default.Send(new Msg_Logout());
}
