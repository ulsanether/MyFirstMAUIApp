using MauiFirstUartApp.ViewModels;

namespace MauiFirstUartApp.Views;

public partial class LoadingPage : ContentPage
{
    private readonly MainPageViewModel _viewModel;

    public LoadingPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
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
            // 로딩 텍스트 업데이트
            LoadingText.Text = "시리얼 포트를 검색하는 중...";
            await Task.Delay(500);

            // ViewModel 초기화
            await _viewModel.InitializeAsync();
            LoadingText.Text = "설정을 로드하는 중...";
            await Task.Delay(500);

            // 추가 초기화 작업들
            LoadingText.Text = "UI를 준비하는 중...";
            await Task.Delay(500);

            LoadingText.Text = "완료!";
            await Task.Delay(300);

            // 메인 셸로 이동
            Application.Current.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            // 오류 처리
            LoadingText.Text = $"초기화 오류: {ex.Message}";
            await Task.Delay(1000);

            // 오류가 있어도 메인 화면으로 이동
            Application.Current.MainPage = new AppShell();
        }
    }
}
