namespace MauiFirstUartApp.Views;

public partial class SerialTerminalPage : ContentPage
{
    public SerialTerminalPage()
    {
        InitializeComponent();
    }

    // "전송" 버튼 클릭 이벤트 핸들러 추가
    private void OnSendClicked(object sender, EventArgs e)
    {
        // 실제 전송 로직은 ViewModel 또는 서비스에서 구현
        // 예시: ViewModel에 SendCommand 호출
        // ((SerialTerminalViewModel)BindingContext)?.SendCommand.Execute(null);
    }
    private void OnClearMessageClicked(object sender, EventArgs e)
    {
        // 실제 전송 로직은 ViewModel 또는 서비스에서 구현
        // 예시: ViewModel에 SendCommand 호출
        // ((SerialTerminalViewModel)BindingContext)?.SendCommand.Execute(null);
    }

    private void OnClearLogsClicked(object sender, EventArgs e)
    {
        // 실제 전송 로직은 ViewModel 또는 서비스에서 구현
        // 예시: ViewModel에 SendCommand 호출
        // ((SerialTerminalViewModel)BindingContext)?.SendCommand.Execute(null);
    }
   

}