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
using System.Diagnostics;
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
        WeakReferenceMessenger.Default.Register<Msg_NewChatEvent>(this, (r, m) =>
        {
            if (m.chat != null) Chats.Add(DTOToModelConverter.ConvertChat(m.chat));
        });
        WeakReferenceMessenger.Default.Register<Msg_NewMessageEvent>(this, (r, m) =>
        {
            if (m.message == null) return;
            MessageModel message = DTOToModelConverter.ConvertMessage(m.message);
            if (TryFindChat(message.Chat, out var chat))
            {
                chat.AddMessage(message);
                if (SelectedChat != chat) chat.UnreadCount++;
            }
        });
        WeakReferenceMessenger.Default.Register<Msg_EditMessageEvent>(this, (r, m) =>
        {
            if (m.message == null) return;
            MessageModel message = DTOToModelConverter.ConvertMessage(m.message);
            if (TryFindChat(message.Chat, out var chat)) chat.EditMessage(message);
        });
        WeakReferenceMessenger.Default.Register<Msg_DeleteMessageEvent>(this, (r, m) =>
        {
            if (m.chat == -1 || m.Id == -1) return;
            if (TryFindChat(m.chat, out var chat)) chat.DeleteMessage(m.Id);
        });
        WeakReferenceMessenger.Default.Register<Msg_SendMessage>(this, (r, m) =>
        {
            if (!TryFindChat(m.to.Id, out var chat))
            {
                Chats.Add(m.to);
                // setting null to actually update ui property and show selection
                SelectedChat = null;
                SelectedChat = m.to;
                ClearSearch();
            }
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
            inSearch = false;
        }
    }
    partial void OnSelectedSearchChanged(ChatModel? value)
    {
        if (String.IsNullOrEmpty(SearchInput)) return;
        if (value == null) return;
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
        Debug.WriteLine(value?.Messages.Count);
        if (value != null) value.UnreadCount = 0;
        if (value?.Messages.Count <= 2 && !inSearch)
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