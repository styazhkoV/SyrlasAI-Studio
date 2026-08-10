using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using SyrlasStudio.Models;
using SyrlasStudio.Services;
using SyrlasAIEngine.Services; // Подключение движка

namespace SyrlasStudio.ViewModels;

public class MainPageViewModel : INotifyPropertyChanged
{
    private readonly LlamaInferenceService _llamaService;
    private readonly PromptFactory _promptFactory;
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

    private bool _isWebSearchEnabled = true;
    public bool IsWebSearchEnabled
    {
        get => _isWebSearchEnabled;
        set => SetProperty(ref _isWebSearchEnabled, value);
    }

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
    public ICommand ExportLogsCommand { get; }

    public MainPageViewModel(ResourceMonitorService? resourceMonitor = null)
    {
        _resourceMonitor = resourceMonitor;
        _llamaService = new LlamaInferenceService();
        _promptFactory = new PromptFactory();
        
        // AgentService принимает LlamaInferenceService, PromptFactory и RagService[cite: 27]
        _agentService = new AgentService(_llamaService, _promptFactory, null!);

        SendMessageCommand = new Command(async () => await SendMessageAsync());
        StopCommand = new Command(OnStop);
        NewChatCommand = new Command(OnNewChat);
        ExportLogsCommand = new Command(async () => await ExportLogsAsync());

        if (_resourceMonitor != null)
        {
            _resourceMonitor.MetricsUpdated += OnMetricsUpdated;
        }

        _ = InitializeEngineAsync();
    }

    private void OnMetricsUpdated(double cpuPercent, double ramMb, double diskPercent, double vramMb)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CpuUsageText = $"{cpuPercent:F1}%";
            CpuProgress = Math.Clamp(cpuPercent / 100.0, 0, 1);
            RamUsageText = $"{ramMb:F0} MB";
            RamProgress = Math.Clamp(ramMb / 16384.0, 0, 1);
            DiskUsageText = $"{diskPercent:F0}%";
            DiskProgress = Math.Clamp(diskPercent / 100.0, 0, 1);
        });
    }

    private async Task InitializeEngineAsync()
    {
        AddLog("Инициализация Syrlas Studio Engine...");
        try
        {
            var modelPath = Path.Combine(FileSystem.AppDataDirectory, "qwen2.5-1.5b-instruct.gguf");
            if (!File.Exists(modelPath))
            {
                modelPath = "qwen2.5-1.5b-instruct.gguf";
            }

            AddLog($"Загрузка модели из: {modelPath}");
            // Загрузка модели в память через LlamaInferenceService[cite: 28]
            await _llamaService.LoadModelAsync(modelPath, contextSize: 4096, gpuLayerCount: 99);
            AddLog("Модель успешно загружена в VRAM.");
        }
        catch (Exception ex)
        {
            AddLog($"Ошибка инициализации модели: {ex.Message}");
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
            // Генерация через AgentService с ролью Системного аналитика[cite: 27, 29]
            var role = AgentRole.SystemAnalyst;
            await foreach (var token in _agentService.ExecuteTaskAsync(role, userText, useRagContext: IsWebSearchEnabled, _cts.Token))
            {
                tokenCount++;
                botMessage.Text += token;

                double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                if (elapsedSeconds > 0.05)
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
            botMessage.Text += " [Остановлено пользователем]";
        }
        catch (Exception ex)
        {
            botMessage.Text += $"\n[Ошибка генерации: {ex.Message}]";
        }
        finally
        {
            stopwatch.Stop();
            IsGenerating = false;

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

    private async Task ExportLogsAsync()
    {
        try
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var filePath = Path.Combine(desktopPath, $"syrlas_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            var content = string.Join(Environment.NewLine, SystemLogs);

            await File.WriteAllTextAsync(filePath, content);
            AddLog($"Лог выгружен на Рабочий стол: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            AddLog($"Ошибка выгрузки лога: {ex.Message}");
        }
    }

    private void OnStop()
    {
        if (IsGenerating && _cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            AddLog("Остановка генерации.");
        }
    }

    private void OnNewChat()
    {
        OnStop();
        Messages.Clear();
        CurrentSpeedText = "0.0 tok/s";
        AddLog("Сессия очищена.");
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