using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SyrlasStudio.Models;
using System.Collections.ObjectModel;

namespace SyrlasStudio.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _selectedRole = "Senior Программист";

    [ObservableProperty]
    private string _codeEditorText = "// Место для сгенерированного кода, BPMN или спецификаций...";

    [ObservableProperty]
    private string _promptInput = string.Empty;

    public ObservableCollection Roles { get; } = new()
    {
        "Бизнес-аналитик",
        "Системный аналитик",
        "Архитектор БД",
        "Архитектор ПО",
        "Senior Программист"
    };

    public ObservableCollection Messages { get; } = new();

    public MainPageViewModel()
    {
        // Стартовое приветственное сообщение
        Messages.Add(new ChatMessage
        {
            Sender = "Syrlas Assistant",
            Text = "Привет! Я готов помочь с архитектурой и кодом. Выберите роль и задайте вопрос.",
            BackgroundColor = "#2D2D2D",
            SenderColor = "#007ACC"
        });
    }

    [RelayCommand]
    private void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(PromptInput))
            return;

        // 1. Добавляем сообщение пользователя
        Messages.Add(new ChatMessage
        {
            Sender = $"Вы ({SelectedRole})",
            Text = PromptInput,
            BackgroundColor = "#0E639C",
            SenderColor = "#FFFFFF"
        });

        string currentPrompt = PromptInput;
        PromptInput = string.Empty; // Очищаем поле ввода

        // 2. Имитация ответа ИИ (заглушка до подключения SyrlasAIEngine)
        Messages.Add(new ChatMessage
        {
            Sender = "Syrlas Assistant",
            Text = $"[Роль: {SelectedRole}]\nОбрабатываю запрос: \"{currentPrompt}\"...",
            BackgroundColor = "#2D2D2D",
            SenderColor = "#007ACC"
        });
    }
}