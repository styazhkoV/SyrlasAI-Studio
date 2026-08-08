using CommunityToolkit.Mvvm.ComponentModel;

namespace SyrlasStudio.Models;

public partial class ChatMessage : ObservableObject
{
    [ObservableProperty]
    private string _sender = string.Empty;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private string _backgroundColor = "#2D2D2D";

    [ObservableProperty]
    private string _senderColor = "#858585";
}