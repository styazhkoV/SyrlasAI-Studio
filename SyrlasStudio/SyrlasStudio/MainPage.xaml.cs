using SyrlasStudio.ViewModels;
using SyrlasStudio.Models;

namespace SyrlasStudio;

public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel _viewModel;

    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.ScrollToRequested += OnScrollToRequested;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Отписываемся от события при уходе со страницы, чтобы избежать утечек памяти
        if (_viewModel != null)
        {
            _viewModel.ScrollToRequested -= OnScrollToRequested;
        }
    }

    private void OnScrollToRequested(ChatMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (message != null && MessagesCollectionView != null)
            {
                MessagesCollectionView.ScrollTo(message, position: ScrollToPosition.End, animate: true);
            }
        });
    }
}