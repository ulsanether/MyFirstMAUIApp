using MauiFirstUartApp.ViewModels;

namespace MauiFirstUartApp.Views;

public partial class LoadingPage : ContentPage
{
    private readonly MainPageViewModel _mainViewModel;

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
            // UI 업데이트: UI 스레드에서
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadingText.Text = "시리얼 포트를 검색하는 중...";
            });

            await _mainViewModel.InitializeAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadingText.Text = "페이지를 미리 생성하는 중...";
            });

            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadingText.Text = "UI를 준비하는 중...";
            });
            await Task.Delay(500);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadingText.Text = "완료!";
            });
            await Task.Delay(300);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current.MainPage = new AppShell();
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadingText.Text = $"초기화 오류: {ex.Message}";
            });
            await Task.Delay(1000);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current.MainPage = new AppShell();
            });
        }
    }

}