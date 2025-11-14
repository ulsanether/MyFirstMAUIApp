using MauiFirstUartApp.ViewModels;

namespace MauiFirstUartApp.Views;

public partial class ModbusPage : ContentPage
{
    private readonly MainPageViewModel _viewModel;

    public ModbusPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    // "읽기" 탭 클릭 이벤트 핸들러 수정
    private void OnReadTabClicked(object sender, EventArgs e)
    {
        _viewModel.IsReadTabSelected = true;
        ReadContent.IsVisible = true;
        WriteContent.IsVisible = false;
        ReadTabButton.BackgroundColor = Colors.White;
        ReadTabButton.TextColor = Color.FromArgb("#1F2937");
        WriteTabButton.BackgroundColor = Colors.Transparent;
        WriteTabButton.TextColor = Color.FromArgb("#64748B");
    }

    // "쓰기" 탭 클릭 이벤트 핸들러 수정
    private void OnWriteTabClicked(object sender, EventArgs e)
    {
        _viewModel.IsReadTabSelected = false;
        ReadContent.IsVisible = false;
        WriteContent.IsVisible = true;
        WriteTabButton.BackgroundColor = Colors.White;
        WriteTabButton.TextColor = Color.FromArgb("#1F2937");
        ReadTabButton.BackgroundColor = Colors.Transparent;
        ReadTabButton.TextColor = Color.FromArgb("#64748B");
    }

   
    private void OnReadExecuteClicked(object sender, EventArgs e)
    {
        _viewModel.ModbusReadCommand.Execute(null);
    }


    private void OnWriteExecuteClicked(object sender, EventArgs e)
    {
        _viewModel.ModbusWriteCommand.Execute(null);
    }
}
