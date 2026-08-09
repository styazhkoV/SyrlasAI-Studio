using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SyrlasStudio.Models;

public class ChatMessage : INotifyPropertyChanged
{
    private string _senderName = string.Empty;
    public string SenderName
    {
        get => _senderName;
        set => SetProperty(ref _senderName, value);
    }

    private string _text = string.Empty;
    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value))
            {
                OnPropertyChanged(nameof(HasCode));
            }
        }
    }

    private bool _isUser;
    public bool IsUser
    {
        get => _isUser;
        set => SetProperty(ref _isUser, value);
    }

    private string _speedText = string.Empty;
    public string SpeedText
    {
        get => _speedText;
        set
        {
            if (SetProperty(ref _speedText, value))
            {
                OnPropertyChanged(nameof(HasSpeedText));
            }
        }
    }

    public bool HasSpeedText => !string.IsNullOrEmpty(SpeedText);

    public bool HasCode => Text.Contains("```");

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value)) return false;
        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}