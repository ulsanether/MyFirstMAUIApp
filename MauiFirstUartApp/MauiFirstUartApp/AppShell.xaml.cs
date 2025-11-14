using MauiFirstUartApp.Views;

namespace MauiFirstUartApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("mainpage", typeof(MainPage));
            Routing.RegisterRoute("settingpage", typeof(SettingPage));
        }
    }
}