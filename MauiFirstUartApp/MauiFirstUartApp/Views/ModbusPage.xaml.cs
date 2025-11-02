namespace MauiFirstUartApp.Views;

public partial class ModbusPage : ContentPage
{
    public ModbusPage()
    {
        InitializeComponent();
    }

    // "읽기" 탭 클릭 이벤트 핸들러 추가
    private void OnReadTabClicked(object sender, EventArgs e)
    {
        ReadContent.IsVisible = true;
        WriteContent.IsVisible = false;
        ReadTabButton.BackgroundColor = Colors.White;
        ReadTabButton.TextColor = Color.FromArgb("#1F2937"); // Gray900
        WriteTabButton.BackgroundColor = Colors.Transparent;
        WriteTabButton.TextColor = Color.FromArgb("#64748B"); // Gray600
    }
    // "쓰기" 탭 클릭 이벤트 핸들러 추가
    private void OnWriteTabClicked(object sender, EventArgs e)
    {
        ReadContent.IsVisible = false;
        WriteContent.IsVisible = true;
        WriteTabButton.BackgroundColor = Colors.White;
        WriteTabButton.TextColor = Color.FromArgb("#1F2937"); // Gray900
        ReadTabButton.BackgroundColor = Colors.Transparent;
        ReadTabButton.TextColor = Color.FromArgb("#64748B"); // Gray600
    }
    // "읽기 실행" 버튼 클릭 이벤트 핸들러 추가
    private void OnReadExecuteClicked(object sender, EventArgs e)
    {
        // 실제 읽기 동작은 ViewModel 또는 서비스에서 구현
        // 예시: ViewModel에 ReadCommand 호출
        // ((ModbusViewModel)BindingContext)?.ReadCommand.Execute(null);
    }

    private void OnWriteExecuteClicked(object sender, EventArgs e)
    {
        // 실제 쓰기 동작은 ViewModel 또는 서비스에서 구현
        // 예시: ViewModel에 WriteCommand 호출
        // ((ModbusViewModel)BindingContext)?.WriteCommand.Execute(null);
    }

}