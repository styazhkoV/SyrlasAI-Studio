using CommunityToolkit.Mvvm.ComponentModel;

namespace SyrlasStudio.Models;

public partial class ChatMessage : ObservableObject
{
    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private string _senderName = string.Empty;

    [ObservableProperty]
    private bool _isUser;

    [ObservableProperty]
    private bool _hasCode;
}