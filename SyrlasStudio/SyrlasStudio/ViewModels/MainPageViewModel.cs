using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SyrlasAIEngine.Services;
using SyrlasStudio.Models;
using System.Collections.ObjectModel;

namespace SyrlasStudio.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly AgentService _agentService;

    // Событие для оповещения View о необходимости прокрутки
    public event Action<ChatMessage>? ScrollToRequested;

    // ... свойства (SelectedRole, CodeEditorText, PromptInput и т.д.) ...

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(PromptInput) || IsGenerating)
            return;

        string userPrompt = PromptInput;
        string role = SelectedRole;

        // 1. Добавляем сообщение пользователя
        var userMessage = new ChatMessage
        {
            Sender = $"Вы ({role})",
            Text = userPrompt,
            BackgroundColor = "#0E639C",
            SenderColor = "#FFFFFF"
        };
        Messages.Add(userMessage);
        
        // Скроллим к сообщению пользователя
        ScrollToRequested?.Invoke(userMessage);

        PromptInput = string.Empty;
        IsGenerating = true;

        // 2. Создаем заготовку ответа ИИ
        var aiMessage = new ChatMessage
        {
            Sender = $"Syrlas Assistant ({role})",
            Text = string.Empty,
            BackgroundColor = "#2D2D2D",
            SenderColor = "#007ACC"
        };
        Messages.Add(aiMessage);
        
        // Скроллим к новому блоку ответа
        ScrollToRequested?.Invoke(aiMessage);

        try
        {
            // 3. Потоковый прием токенов
            await foreach (var token in _agentService.ExecuteTaskStreamAsync(role, userPrompt))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    aiMessage.Text += token;
                    
                    // Поворачиваем скролл вниз при добавлении каждого токена
                    ScrollToRequested?.Invoke(aiMessage);
                });
            }
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                aiMessage.Text += $"\n\n⚠️ Ошибка генерации: {ex.Message}";
                ScrollToRequested?.Invoke(aiMessage);
            });
        }
        finally
        {
            IsGenerating = false;
        }
    }
}