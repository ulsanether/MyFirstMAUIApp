namespace MauiFirstUartApp.Views;

public partial class SettingPage : ContentPage
{
	public SettingPage()
	{
		InitializeComponent();
	}
    // 이벤트 핸들러 추가
    private void OnModbusProtocolChanged(object sender, EventArgs e)
    {
        // 필요에 따라 프로토콜 변경 시 동작 구현
        var picker = sender as Picker;
        if (picker != null && picker.SelectedItem != null)
        {
            string selectedProtocol = picker.SelectedItem.ToString();
            // 예시: TCP 관련 UI 표시/숨김 처리 (현재 TCP 항목 없음)
            // TcpSettingsStack.IsVisible = selectedProtocol.Contains("TCP");
        }
    }

}