using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SyrlasStudio.Models;
using SyrlasStudio.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace SyrlasStudio.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly AgentService _agentService;
    private CancellationTokenSource? _cts; // Источник токена отмены

    public event Action<ChatMessage>? ScrollToRequested;

#pragma warning disable MVVMTK0045
    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = new();

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _codeEditorText = "// Ваш код появится здесь после применения ответа ИИ\nusing System;\n\nnamespace SyrlasStudio;\n";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotGenerating))] // Автоматически обновляет обратное свойство
    private bool _isGenerating;

    public bool IsNotGenerating => !IsGenerating; // Индикатор для видимости кнопки отправки

    [ObservableProperty]
    private string _modelPath = @"C:\Users\alexs\SyrlasStudio\SyrlasAIEngine\Model\qwen2.5-14b-instruct-uncensored-q5_k_m.gguf";
#pragma warning restore MVVMTK0045

    public MainPageViewModel(AgentService agentService)
    {
        _agentService = agentService;
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || IsGenerating)
            return;

        string prompt = InputText;
        InputText = string.Empty;
        IsGenerating = true;

        // Создаем новый токен для текущей генерации
        _cts = new CancellationTokenSource();

        var userMsg = new ChatMessage
        {
            SenderName = "Вы (Разработчик)",
            Text = prompt,
            IsUser = true
        };
        Messages.Add(userMsg);
        ScrollToRequested?.Invoke(userMsg);

        var aiMsg = new ChatMessage
        {
            SenderName = "Syrlas Architect (Qwen 2.5 14B)",
            Text = string.Empty,
            IsUser = false
        };
        Messages.Add(aiMsg);
        ScrollToRequested?.Invoke(aiMsg);

        try
        {
            // Передаем токен отмены в сервис генерации
            await foreach (string token in _agentService.GenerateResponseAsync(prompt, _cts.Token))
            {
                aiMsg.Text += token;
                ScrollToRequested?.Invoke(aiMsg);
            }
        }
        catch (OperationCanceledException)
        {
            aiMsg.Text += "\n\n⏹️ [Генерация остановлена пользователем]";
        }
        catch (Exception ex)
        {
            aiMsg.Text += $"\n\n❌ [Ошибка AI]: {ex.Message}";
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            IsGenerating = false;
        }
    }

    // Команда для остановки генерации
    [RelayCommand]
    private void StopGeneration()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    private void ApplyCodeToFile(ChatMessage message)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.Text))
            return;

        string extractedCode = CodeBlockExtractor.ExtractCode(message.Text);

        if (!string.IsNullOrWhiteSpace(extractedCode))
        {
            CodeEditorText = extractedCode;
            message.IsApplied = true;
            message.AppliedStatusText = "✓ Применено в редактор";
        }
    }
}