using Microsoft.Extensions.Logging;
using MauiFirstUartApp.Core.Abstractions;
using MauiFirstUartApp.ViewModels;
using MauiFirstUartApp.Views;

#if WINDOWS
using MauiFirstUartApp.Platforms.Windows;
#elif ANDROID
using MauiFirstUartApp.Platforms.Android;
#endif

namespace MauiFirstUartApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
                fonts.AddFont("MaterialIconsOutlined-Regular.otf", "MaterialIconsOutlined");
            });

        // 서비스 등록
        RegisterServices(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // 플랫폼별 서비스 등록
#if WINDOWS
        services.AddSingleton<ISerialService, Platforms.Windows.SerialService>();
#elif ANDROID
        services.AddSingleton<ISerialService, Platforms.Android.SerialService>();
#endif

        // 비즈니스 서비스
     //   services.AddSingleton<UartCommunicationService>();

        // ViewModels
        services.AddSingleton<MainPageViewModel>();

        // Pages
        services.AddTransient<MainPage>();
        services.AddTransient<SerialTerminalPage>();
        services.AddTransient<ModbusPage>();
        services.AddTransient<SettingPage>();
        services.AddTransient<InfoPage>();
        services.AddTransient<LoadingPage>(); 
    }
}
