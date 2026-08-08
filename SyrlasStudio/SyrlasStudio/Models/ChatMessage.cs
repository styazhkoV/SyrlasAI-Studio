using CommunityToolkit.Mvvm.ComponentModel;
using SyrlasStudio.Services;

namespace SyrlasStudio.Models;

public partial class ChatMessage : ObservableObject
{
#pragma warning disable MVVMTK0045
    [ObservableProperty]
    private string _senderName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCode))]
    private string _text = string.Empty;

    [ObservableProperty]
    private bool _isUser;

    [ObservableProperty]
    private bool _isApplied;

    [ObservableProperty]
    private string _appliedStatusText = "Применить в редактор";
#pragma warning restore MVVMTK0045

    public bool HasCode => CodeBlockExtractor.ContainsCodeBlock(Text);
}