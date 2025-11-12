using MauiFirstUartApp.ViewModels;

namespace MauiFirstUartApp.Views;

public partial class SettingPage : ContentPage
{
    public SettingPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel; // 같은 인스턴스 공유
    }

    private void OnModbusProtocolChanged(object sender, EventArgs e)
    {
        var picker = sender as Picker;
        if (picker != null && picker.SelectedItem != null)
        {
            string selectedProtocol = picker.SelectedItem.ToString();
            // 필요시 UI 처리
        }
    }
}
