using CommunityToolkit.Mvvm.ComponentModel;
using PPgram.Shared;

namespace PPgram.MVVM.Models.Item
{
    internal abstract class ChatItem : ObservableObject
    {
        public ItemType Type { get; set; }
    }
}
