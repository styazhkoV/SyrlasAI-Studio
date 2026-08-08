using SyrlasStudio.ViewModels;

namespace SyrlasStudio;

public partial class MainPage : ContentPage
{
    // DI автоматически передаст сконфигурированную MainPageViewModel
    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}