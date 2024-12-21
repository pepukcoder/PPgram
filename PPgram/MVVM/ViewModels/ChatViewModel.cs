using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using PPgram.App;
using PPgram.Helpers;
using PPgram.MVVM.Models.Chat;
using PPgram.MVVM.Models.Dialog;
using PPgram.MVVM.Models.File;
using PPgram.MVVM.Models.Item;
using PPgram.MVVM.Models.Media;
using PPgram.MVVM.Models.Message;
using PPgram.MVVM.Models.MessageContent;
using PPgram.MVVM.Models.User;
using PPgram.Net.DTO;
using PPgram.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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
        WeakReferenceMessenger.Default.Register<Msg_FetchChatsResult>(this, (r, e) =>
        {
            foreach (ChatDTO chat in e.chats)
            {
                Chats.Add(DTOToModelConverter.ConvertChat(chat));
            }
        });
        //WeakReferenceMessenger.Default.Register<Msg_ChangeMessageStatus>(this, (r, e) => ChangeMessageStatus(e.chat, e.Id, e.status));
        WeakReferenceMessenger.Default.Register<Msg_DeleteMessageEvent>(this, (r, e) =>
        {
            ObservableCollection<ChatItem>? messages = Chats.FirstOrDefault(c => c.Id == e.chat)?.Messages;
            if (messages == null) return;
            MessageModel? originalMessage = messages.OfType<MessageModel>().FirstOrDefault(m => m.Id == e.Id);
            if (originalMessage == null) return;
            chainManager.DeleteChain(originalMessage, messages);
        });
    }
    partial void OnSearchInputChanged(string value)
    {
        // stop timer when editing search query
        _timer.Stop();
        // restart delay if username is not null
        // clean searchlist if search closed
        if (!String.IsNullOrEmpty(value.Trim()))
        {
            _timer.Start();
            inSearch = true;
        }
        else
        {
            inSearch = false;
        }
    }
    private void SearchChat(object? sender, EventArgs e)
    {
        // stop timer to prevent request spam
        _timer.Stop();
        WeakReferenceMessenger.Default.Send(new Msg_SearchChats { searchQuery = SearchInput.Trim() });
    }
    public void OnSearchListSelectedChanged(ChatModel? value)
    {
        /*if (String.IsNullOrEmpty(SearchInput)) return;
        if (ChatList.Any(c => c.Id == value?.Id))
        {
            inSearch = false;
            ChatListSelected = ChatList.FirstOrDefault(c => c.Id == value?.Id) ?? ChatList[0];
            ClearSearch();
        }
        else
        {
            CurrentProfile = value?.Profile ?? CurrentProfile;
            MessageList = [];
        }*/
    }
    partial void OnSelectedChatChanged(ChatModel? value)
    {
        if (value?.Messages.Count == 0 && !inSearch)
        {
            WeakReferenceMessenger.Default.Send(new Msg_FetchMessages()
            {
                chatId = value.Id,
            });
        }
    }
    /*
    public void UpdateSearch(ObservableCollection<ChatModel> resultList)
    {
        SearchList = resultList;
        SearchListSelected = null;
    }
    public void UpdateChats(ObservableCollection<ChatModel> chatList) => ChatList = chatList;
    public void UpdateMessages(ObservableCollection<MessageModel> messageList)
    {
        if (ChatListSelected == null) return;
        MessageList = ChatListSelected.Messages = chainManager.GenerateChain(messageList, ChatListSelected);
    }
    public void AddChat(ChatModel chat) => ChatList.Add(chat);
    public void AddMessage(MessageModel message)
    {
        ObservableCollection<ChatItem>? messages = ChatList.FirstOrDefault(c => c.Id == message.Chat)?.Messages;
        if (messages == null) return;
        messages.Add(message);
        chainManager.AddChain(messages);
    }
    public void ChangeMessageStatus(int chat, int id, MessageStatus status)
    {
        MessageModel? message = ChatList.FirstOrDefault(c => c.Id == chat)?.Messages.OfType<MessageModel>().LastOrDefault();
        if (message != null)
        {
            message.Id = id;
            message.Status = status;
        }
    }
    [RelayCommand]
    private void ClearSearch()
    {
        SearchInput = string.Empty;
        inSearch = false;
    }*/
    [RelayCommand]
    private void CloseChat() => SelectedChat = null;
    [RelayCommand]
    private void Logout() => WeakReferenceMessenger.Default.Send(new Msg_Logout());
}