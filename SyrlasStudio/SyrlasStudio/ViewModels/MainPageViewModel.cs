using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using SyrlasStudio.Models;
using SyrlasStudio.Services;

namespace SyrlasStudio.ViewModels;

public class MainPageViewModel : INotifyPropertyChanged
{
    private readonly AgentService _agentService;
    private readonly ResourceMonitorService? _resourceMonitor;
    private CancellationTokenSource? _cts;

    public event Action? ScrollToRequested;

    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public ObservableCollection<string> SystemLogs { get; } = new();

    private string _userInputText = string.Empty;
    public string UserInputText
    {
        get => _userInputText;
        set => SetProperty(ref _userInputText, value);
    }

    private bool _isGenerating;
    public bool IsGenerating
    {
        get => _isGenerating;
        set
        {
            if (SetProperty(ref _isGenerating, value))
            {
                OnPropertyChanged(nameof(IsNotGenerating));
            }
        }
    }

    public bool IsNotGenerating => !IsGenerating;

    private string _currentSpeedText = "0.0 tok/s";
    public string CurrentSpeedText
    {
        get => _currentSpeedText;
        set => SetProperty(ref _currentSpeedText, value);
    }

    private string _cpuUsageText = "0,0%";
    public string CpuUsageText
    {
        get => _cpuUsageText;
        set => SetProperty(ref _cpuUsageText, value);
    }

    private double _cpuProgress = 0.0;
    public double CpuProgress
    {
        get => _cpuProgress;
        set => SetProperty(ref _cpuProgress, value);
    }

    private string _ramUsageText = "0 MB";
    public string RamUsageText
    {
        get => _ramUsageText;
        set => SetProperty(ref _ramUsageText, value);
    }

    private double _ramProgress = 0.0;
    public double RamProgress
    {
        get => _ramProgress;
        set => SetProperty(ref _ramProgress, value);
    }

    private string _diskUsageText = "0%";
    public string DiskUsageText
    {
        get => _diskUsageText;
        set => SetProperty(ref _diskUsageText, value);
    }

    private double _diskProgress = 0.0;
    public double DiskProgress
    {
        get => _diskProgress;
        set => SetProperty(ref _diskProgress, value);
    }

    private float _temperature = 0.7f;
    public float Temperature
    {
        get => _temperature;
        set
        {
            if (SetProperty(ref _temperature, value))
            {
                OnPropertyChanged(nameof(TemperatureText));
            }
        }
    }

    private float _topP = 0.9f;
    public float TopP
    {
        get => _topP;
        set
        {
            if (SetProperty(ref _topP, value))
            {
                OnPropertyChanged(nameof(TopPText));
            }
        }
    }

    public string TemperatureText => $"{Temperature:F2}";
    public string TopPText => $"{TopP:F2}";

    public ICommand SendMessageCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand NewChatCommand { get; }
    public ICommand CopyCodeCommand { get; }

    public MainPageViewModel(AgentService agentService, ResourceMonitorService? resourceMonitor = null)
    {
        _agentService = agentService;
        _resourceMonitor = resourceMonitor;

        SendMessageCommand = new Command(async () => await SendMessageAsync());
        StopCommand = new Command(OnStop);
        NewChatCommand = new Command(OnNewChat);
        CopyCodeCommand = new Command<string>(async (code) => await CopyCodeAsync(code));

        if (_resourceMonitor != null)
        {
            _resourceMonitor.MetricsUpdated += OnMetricsUpdated;
        }

        _ = InitializeEngineAsync();
    }

    public MainPageViewModel() : this(new AgentService())
    {
    }

    private void OnMetricsUpdated(double cpuPercent, double ramMb, double diskPercent, double vramMb)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CpuUsageText = $"{cpuPercent:F1}%";
            CpuProgress = Math.Clamp(cpuPercent / 100.0, 0, 1);
            RamUsageText = $"{ramMb:F0} MB";
            // Нормализуем относительно ~16 GB; UI только индикатор
            RamProgress = Math.Clamp(ramMb / 16384.0, 0, 1);
            DiskUsageText = $"{diskPercent:F0}%";
            DiskProgress = Math.Clamp(diskPercent / 100.0, 0, 1);
        });
    }

    private async Task InitializeEngineAsync()
    {
        AddLog("Система Syrlas Studio инициализирована.");
        try
        {
            await _agentService.InitializeAsync(AddLog);
        }
        catch (Exception ex)
        {
            AddLog($"Ошибка инициализации: {ex.Message}");
        }
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInputText) || IsGenerating) return;

        var userText = UserInputText;
        UserInputText = string.Empty;
        IsGenerating = true;

        _cts = new CancellationTokenSource();

        Messages.Add(new ChatMessage
        {
            SenderName = "Вы",
            Text = userText,
            IsUser = true
        });

        var botMessage = new ChatMessage
        {
            SenderName = "Syrlas AI Assistant",
            Text = string.Empty,
            IsUser = false
        };
        Messages.Add(botMessage);

        RequestScroll();

        var stopwatch = Stopwatch.StartNew();
        int tokenCount = 0;

        try
        {
            await foreach (var token in _agentService.GenerateResponseAsync(userText, Temperature, TopP, _cts.Token))
            {
                tokenCount++;
                botMessage.Text += token;

                // Замер скорости в реальном времени
                double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                if (elapsedSeconds > 0.1)
                {
                    double currentSpeed = tokenCount / elapsedSeconds;
                    CurrentSpeedText = $"{currentSpeed:F1} tok/s";
                    botMessage.SpeedText = $"{currentSpeed:F1} tok/s";
                }

                RequestScroll();
            }
        }
        catch (OperationCanceledException)
        {
            botMessage.Text += " [Генерация остановлена пользователем]";
        }
        catch (Exception ex)
        {
            botMessage.Text += $"\n[Ошибка генерации: {ex.Message}]";
        }
        finally
        {
            stopwatch.Stop();
            IsGenerating = false;

            // Финальный точный расчет скорости
            if (stopwatch.Elapsed.TotalSeconds > 0 && tokenCount > 0)
            {
                double finalSpeed = tokenCount / stopwatch.Elapsed.TotalSeconds;
                CurrentSpeedText = $"{finalSpeed:F1} tok/s";
                botMessage.SpeedText = $"{finalSpeed:F1} tok/s • {tokenCount} токенов за {stopwatch.Elapsed.TotalSeconds:F2}с";
            }

            _cts?.Dispose();
            _cts = null;
            RequestScroll();
        }
    }

    private void OnStop()
    {
        if (IsGenerating && _cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            AddLog("Запрос на остановку генерации выслан.");
        }
    }

    private void OnNewChat()
    {
        OnStop();
        Messages.Clear();
        CurrentSpeedText = "0.0 tok/s";
        AddLog("Сессия очищена.");
    }

    private async Task CopyCodeAsync(string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            await Clipboard.Default.SetTextAsync(text);
        }
    }

    private void AddLog(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SystemLogs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        });
    }

    private void RequestScroll()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ScrollToRequested?.Invoke();
        });
    }

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