using MauiFirstUartApp.ViewModels;
using MauiFirstUartApp.Views;
namespace MauiFirstUartApp.Views;

public partial class LoadingPage : ContentPage
{
    private readonly MainPageViewModel _mainViewModel;

    private SerialTerminalPage _serialTerminalPage;
    private ModbusPage _modbusPage;
    private SettingPage _settingPage;

    public LoadingPage(
        MainPageViewModel mainViewModel)
    {
        InitializeComponent();
        _mainViewModel = mainViewModel;
       
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeApp();
    }


    private async Task InitializeApp()
    {
        try
        {
            LoadingText.Text = "시리얼 포트를 검색하는 중...";
            await _mainViewModel.InitializeAsync();

            LoadingText.Text = "페이지를 미리 생성하는 중...";
            _serialTerminalPage = new SerialTerminalPage(_mainViewModel);
            _modbusPage = new ModbusPage(_mainViewModel);
            _settingPage = new SettingPage(_mainViewModel);

            LoadingText.Text = "UI를 준비하는 중...";
            await Task.Delay(500);

            LoadingText.Text = "완료!";
            await Task.Delay(300);

            Application.Current.MainPage = new AppShell();
            // AppShell에서 미리 생성한 페이지를 활용하도록 구현 필요
        }
        catch (Exception ex)
        {
            LoadingText.Text = $"초기화 오류: {ex.Message}";
            await Task.Delay(1000);
            Application.Current.MainPage = new AppShell();
        }
    }

}
