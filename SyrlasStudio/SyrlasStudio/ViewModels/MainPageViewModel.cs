using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SyrlasStudio.Models;
using SyrlasStudio.Services;
using System.Collections.ObjectModel;

namespace SyrlasStudio.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    public event Action<ChatMessage>? ScrollToRequested;

#pragma warning disable MVVMTK0045
    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = new();

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _codeEditorText = "// Ваш код появится здесь после выбора или применения ответа ИИ\nusing System;\n\nnamespace SyrlasStudio;\n";

    [ObservableProperty]
    private bool _isGenerating;
#pragma warning restore MVVMTK0045

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || IsGenerating)
            return;

        string prompt = InputText;
        InputText = string.Empty;
        IsGenerating = true;

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
            SenderName = "Syrlas Architect (Qwen 2.5 Coder)",
            Text = string.Empty,
            IsUser = false
        };
        Messages.Add(aiMsg);
        ScrollToRequested?.Invoke(aiMsg);

        string sampleResponse = "Отличное решение! Вот обновленная реализация метода:\n\n```csharp\npublic void ProcessData()\n{\n    Console.WriteLine(\"Syrlas Core Engine Processing...\");\n}\n```";
        
        foreach (char c in sampleResponse)
        {
            aiMsg.Text += c;
            ScrollToRequested?.Invoke(aiMsg);
            await Task.Delay(15);
        }

        IsGenerating = false;
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