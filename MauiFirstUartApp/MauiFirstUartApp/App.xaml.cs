using MauiFirstUartApp.Views;
using MauiFirstUartApp.ViewModels;

namespace MauiFirstUartApp
{
    public partial class App : Application
    {



        public App()
        {
            InitializeComponent();

            UserAppTheme = AppTheme.Unspecified;

            // LoadingPage를 첫 화면으로 설정
            var viewModel = ServiceHelper.GetService<MainPageViewModel>();

            MainPage = new LoadingPage(viewModel);
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);

            const int newWidth = 800;
            const int newHeight = 600;

            window.Width = newWidth;
            window.Height = newHeight;

            return window;
        }
    }

    // 서비스 헬퍼 클래스
    public static class ServiceHelper
    {
        public static TService GetService<TService>()
            => Current.GetService<TService>();

        public static IServiceProvider Current =>
#if WINDOWS10_0_17763_0_OR_GREATER
            MauiWinUIApplication.Current.Services;
#elif ANDROID
            MauiApplication.Current.Services;
#elif IOS || MACCATALYST
            MauiUIApplicationDelegate.Current.Services;
#else
            null;
#endif
    }
}
