using MauiFirstUartApp.ViewModels;

namespace MauiFirstUartApp.Views;

public partial class SerialTerminalPage : ContentPage
{
    private readonly MainPageViewModel _viewModel;

    public SerialTerminalPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
}