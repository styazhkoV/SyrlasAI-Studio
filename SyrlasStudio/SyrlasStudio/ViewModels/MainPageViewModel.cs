using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using SyrlasStudio.Services;
using SyrlasStudio.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace SyrlasStudio.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly AgentService _agentService;
    private readonly ResourceMonitorService _monitorService;
    private CancellationTokenSource? _cts;

    // Событие для прокрутки чата вниз
    public event Action<ChatMessage>? ScrollToRequested;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = new();

    [ObservableProperty]
    private string _userInputText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _systemLogs = new();

    // Метрики ресурсов системы и GPU
    [ObservableProperty]
    private string _cpuUsageText = "CPU: 0%";

    [ObservableProperty]
    private double _cpuProgress = 0.0;

    [ObservableProperty]
    private string _ramUsageText = "RAM: 0 MB";

    [ObservableProperty]
    private double _ramProgress = 0.0;

    [ObservableProperty]
    private string _diskUsageText = "Disk: 0%";

    [ObservableProperty]
    private double _diskProgress = 0.0;

    [ObservableProperty]
    private string _vramUsageText = "VRAM: 0 MB";

    [ObservableProperty]
    private double _vramProgress = 0.0;

    public MainPageViewModel(AgentService agentService)
    {
        _agentService = agentService;

        _monitorService = new ResourceMonitorService();
        _monitorService.MetricsUpdated += OnMetricsUpdated;

        Log("Система Syrlas Studio инициализирована.");
        Log("Готов к загрузке весов...");
    }

    private void OnMetricsUpdated(double cpuPercent, double ramMb, double diskPercent, double vramMb)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CpuUsageText = $"CPU: {cpuPercent:F1}%";
            CpuProgress = Math.Min(cpuPercent / 100.0, 1.0);

            RamUsageText = $"RAM: {ramMb:F0} MB";
            RamProgress = Math.Min(ramMb / 16384.0, 1.0); // Шкала на 16 ГБ RAM

            DiskUsageText = $"Disk: {diskPercent:F0}%";
            DiskProgress = Math.Min(diskPercent / 100.0, 1.0);

            // GTX 1070 имеет 8192 MB VRAM
            VramUsageText = $"VRAM: {vramMb:F0} MB";
            VramProgress = Math.Min(vramMb / 8192.0, 1.0);
        });
    }

    public void Log(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string logEntry = $"[{timestamp}] {message}";

        MainThread.BeginInvokeOnMainThread(() =>
        {
            SystemLogs.Add(logEntry);
            if (SystemLogs.Count > 50)
            {
                SystemLogs.RemoveAt(0);
            }
        });
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInputText)) return;

        string prompt = UserInputText;
        UserInputText = string.Empty;

        var userMsg = new ChatMessage { Text = prompt, IsUser = true, SenderName = "Вы" };
        Messages.Add(userMsg);
        ScrollToRequested?.Invoke(userMsg);

        var aiMsg = new ChatMessage { Text = "", IsUser = false, SenderName = "Syrlas AI Assistant" };
        Messages.Add(aiMsg);
        ScrollToRequested?.Invoke(aiMsg);

        Log("Запрос отправлен в локальный движок...");
        
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            await foreach (string token in _agentService.GenerateResponseAsync(prompt, _cts.Token))
            {
                aiMsg.Text += token;
            }
            Log("Генерация ответа успешно завершена.");
        }
        catch (OperationCanceledException)
        {
            Log("Прерывание: Операция отменена.");
        }
        catch (Exception ex)
        {
            Log($"ОШИБКА ДВИЖКА: {ex.Message}");
            aiMsg.Text += $"\n[Ошибка: {ex.Message}]";
        }
    }

    [RelayCommand]
    private void NewChat()
    {
        _cts?.Cancel();
        Messages.Clear();
        Log("Начат новый диалог.");
    }

    [RelayCommand]
    private async Task CopyCodeAsync(string code)
    {
        if (!string.IsNullOrEmpty(code))
        {
            await Clipboard.Default.SetTextAsync(code);
            Log("Код скопирован в буфер обмена.");
        }
    }
}