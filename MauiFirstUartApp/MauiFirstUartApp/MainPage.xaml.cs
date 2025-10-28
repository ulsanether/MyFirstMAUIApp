using MauiFirstUartApp.Core.Abstractions;

namespace MauiFirstUartApp
{
    public partial class MainPage : ContentPage
    {
        private readonly ISerialService _serialService;
        private CancellationTokenSource? _readCts;

        public MainPage(ISerialService serialService)
        {
            InitializeComponent();
            _serialService = serialService;
            ParityPicker.SelectedIndex = 0;
            StopBitsPicker.SelectedIndex = 0;
            LoadPorts();
        }

        private async void LoadPorts()
        {
            var ports = await _serialService.GetDeviceNamesAsync();
            var portList = ports.ToList();
            PortPicker.ItemsSource = portList;
            if (portList.Count > 0)
                PortPicker.SelectedIndex = 0;
        }

        private async void OnConnectClicked(object sender, EventArgs e)
        {
            try
            {
                var portName = PortPicker.SelectedItem as string;
                int baud = int.TryParse(BaudEntry.Text, out var b) ? b : 115200;
                int dataBits = int.TryParse(DataBitsEntry.Text, out var d) ? d : 8;
                // Enum 변환
                var parity = (SerialParity)(ParityPicker.SelectedIndex >= 0 ? ParityPicker.SelectedIndex : 0);
                var stopBits = (SerialStopBits)(StopBitsPicker.SelectedIndex >= 0 ? StopBitsPicker.SelectedIndex : 0);

                await _serialService.OpenAsync(portName, baud, dataBits, (int)stopBits, (int)parity);

                ConnectBtn.IsEnabled = false;
                DisconnectBtn.IsEnabled = true;
                SendBtn.IsEnabled = true;
                StatusLabel.Text = "상태: 연결됨";

                _readCts = new CancellationTokenSource();
                ReceiveEditor.Text = "";
                _ = ReadLoopAsync(_readCts.Token);
            }
            catch (Exception ex)
            {
                await DisplayAlert("연결 오류", ex.Message, "확인");
            }
        }

        private async Task ReadLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var data = await _serialService.ReadAsync(ct);
                if (data != null && data.Length > 0)
                {
                    ReceiveEditor.Text += System.Text.Encoding.UTF8.GetString(data);
                }
                await Task.Delay(50, ct);
            }
        }

        private async void OnDisconnectClicked(object sender, EventArgs e)
        {
            _readCts?.Cancel();
            await _serialService.CloseAsync();
            ConnectBtn.IsEnabled = true;
            DisconnectBtn.IsEnabled = false;
            SendBtn.IsEnabled = false;
            StatusLabel.Text = "상태: 연결 안됨";
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            try
            {
                var text = SendEntry.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(text);
                    await _serialService.WriteAsync(bytes);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("전송 오류", ex.Message, "확인");
            }
        }
    }
}
