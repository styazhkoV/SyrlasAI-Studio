using SyrlasStudio.ViewModels;

namespace SyrlasStudio;

public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel _viewModel;

    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;

        // Автоматический скролл вниз при получении новых токенов
        _viewModel.ScrollToRequested += OnScrollToRequested;
    }

    public MainPage() : this(new MainPageViewModel())
    {
    }

    private void OnScrollToRequested()
    {
        if (_viewModel?.Messages != null && _viewModel.Messages.Count > 0)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MessagesCollectionView.ScrollTo(_viewModel.Messages.Count - 1, animate: true);
            });
        }
    }
}