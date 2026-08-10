using Microsoft.Maui.Controls;
using SyrlasStudio.ViewModels;

namespace SyrlasStudio;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Подписка на автоматическую прокрутку чата при поступлении новых токенов
        viewModel.ScrollToRequested += OnScrollToRequested;
    }

    private void OnScrollToRequested()
    {
        if (BindingContext is MainPageViewModel vm && vm.Messages.Count > 0)
        {
            MessagesCollectionView.ScrollTo(vm.Messages.Count - 1, position: ScrollToPosition.End, animate: true);
        }
    }
}