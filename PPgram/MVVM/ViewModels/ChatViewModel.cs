using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.App;
using PPgram.Helpers;
using PPgram.MVVM.Models.Chat;
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
    private bool foldersVisible;
    [ObservableProperty]
    private bool platesVisible;

    private readonly DispatcherTimer _timer;
    private readonly MessageChainManager chainManager = new();
    private readonly ReplyModel reply = new();
    private bool inSearch;

    public ChatViewModel()
    {
        FoldersVisible = true;
        PlatesVisible = false;

        // search request delay timer
        _timer = new() { Interval = TimeSpan.FromMilliseconds(25) };
        _timer.Tick += SearchChat;
        // events
        WeakReferenceMessenger.Default.Register<Msg_NewChatEvent>(this, (r, e) =>
        {
            if (e.chat != null) Chats.Add(DTOToModelConverter.ConvertChat(e.chat));
        });
        WeakReferenceMessenger.Default.Register<Msg_NewMessageEvent>(this, (r, e) =>
        {
            if (e.message == null) return;
            MessageModel message = DTOToModelConverter.ConvertMessage(e.message);
            if (TryFindChat(message.Chat, out var chat))
            {
                chat.AddMessage(message);
                if (SelectedChat != chat) chat.UnreadCount++;
            }
        });
        WeakReferenceMessenger.Default.Register<Msg_EditMessageEvent>(this, (r, e) =>
        {
            if (e.message == null) return;
            MessageModel message = DTOToModelConverter.ConvertMessage(e.message);
            if (TryFindChat(message.Chat, out var chat)) chat.EditMessage(message);
        });
        WeakReferenceMessenger.Default.Register<Msg_DeleteMessageEvent>(this, (r, e) =>
        {
            if (e.chat == -1 || e.Id == -1) return;
            if (TryFindChat(e.chat, out var chat)) chat.DeleteMessage(e.Id);
        });
    }
    partial void OnSearchInputChanged(string value)
    {
        // stop timer if still editing
        _timer.Stop();
        if (!String.IsNullOrEmpty(value.Trim()))
        {
            // restart delay if done editing
            _timer.Start();
            inSearch = true;
        }
        else
        {
            SearchResults = [];
            inSearch = false;
        }
    }
    partial void OnSelectedSearchChanged(ChatModel? value)
    {
        if (String.IsNullOrEmpty(SearchInput)) return;
        if (Chats.Any(c => c.Id == value?.Id))
        {
            inSearch = false;
            SelectedChat = Chats.FirstOrDefault(c => c.Id == value?.Id);
            ClearSearch();
        }
        else
        {
            SelectedChat = value;
        }
    }
    partial void OnSelectedChatChanged(ChatModel? value)
    {
        if (value != null) value.UnreadCount = 0;
        if (value?.Messages.Count == 0 && !inSearch)
        {
            Msg_FetchMessages msg = new() { chatId = value.Id };
            WeakReferenceMessenger.Default.Send(msg);
        }
    }
    private void SearchChat(object? sender, EventArgs e)
    {
        // stop timer to prevent request spam
        _timer.Stop();
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
    public void UpdateChats(ObservableCollection<ChatModel> chats) => Chats = chats;
    public void UpdateMessages(int chat_id, List<MessageModel> messages)
    {
        if (TryFindChat(chat_id, out var chat)) chat.LoadMessages(messages);
    }
    public void ChangeMessageStatus(int chat_id, int message_id, MessageStatus status)
    {
        if (TryFindChat(chat_id, out var chat)) chat.ChangeMessageStatus(message_id, status);
    }
    [RelayCommand]
    private void ClearSearch() => SearchInput = string.Empty;
    [RelayCommand]
    private void CloseChat() => SelectedChat = null;
    [RelayCommand]
    private void Logout() => WeakReferenceMessenger.Default.Send(new Msg_Logout());
}