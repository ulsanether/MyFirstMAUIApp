//using AuthenticationServices;

using MauiFirstUartApp.Core.Abstractions;

using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;

namespace MauiFirstUartApp.ViewModels;

public class MainPageViewModel : BindableObject
{
    private readonly ISerialService _serialService;
    private CancellationTokenSource? _readCts;
    
    public ObservableCollection<string> PortNames { get; } = new();
    public ObservableCollection<string> ParityOptions { get; } = new(Enum.GetNames(typeof(SerialParity)));
    public ObservableCollection<string> StopBitsOptions { get; } = new(Enum.GetNames(typeof(SerialStopBits)));


    private byte _modbusSlaveId;
    public byte ModbusSlaveId
    {
        get => _modbusSlaveId;
        set { _modbusSlaveId = value; OnPropertyChanged(); }
    }

    private ushort _modbusAddress;
    public ushort ModbusAddress
    {
        get => _modbusAddress;
        set { _modbusAddress = value; OnPropertyChanged(); }
    }

    private ushort _modbusQuantity;
    public ushort ModbusQuantity
    {
        get => _modbusQuantity;
        set { _modbusQuantity = value; OnPropertyChanged(); }
    }






    private SerialType _selectedSerialType = SerialType.Normal;
    public SerialType SelectedSerialType
    {
        get => _selectedSerialType;
        set
        {
            if (_selectedSerialType != value)
            {
                _selectedSerialType = value;
                OnPropertyChanged();

            
                ((Command)ModbusReadCommand).ChangeCanExecute();
                ((Command)ModbusWriteCommand).ChangeCanExecute();
                ((Command)ModbusReadInputCommand).ChangeCanExecute();


}
        }
    }

    private string? _selectedPort;
    public string? SelectedPort
    {
        get => _selectedPort;
        set { _selectedPort = value; OnPropertyChanged(); }
    }

    private string? _selectedParity;
    public string? SelectedParity
    {
        get => _selectedParity;
        set { _selectedParity = value; OnPropertyChanged(); }
    }

    private string? _selectedStopBits;
    public string? SelectedStopBits
    {
        get => _selectedStopBits;
        set { _selectedStopBits = value; OnPropertyChanged(); }
    }

    private int _baudRate = 115200;
    public int BaudRate
    {
        get => _baudRate;
        set { _baudRate = value; OnPropertyChanged(); }
    }

    private int _dataBits = 8;
    public int DataBits
    {
        get => _dataBits;
        set { _dataBits = value; OnPropertyChanged(); }
    }

    private string? _sendText;
    public string? SendText
    {
        get => _sendText;
        set { _sendText = value; OnPropertyChanged(); }
    }

    private string _receivedText = "";
    public string ReceivedText
    {
        get => _receivedText;
        set { _receivedText = value; OnPropertyChanged(); }
    }

    private string _statusText = "상태: 연결 안됨";
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }
    public ICommand ModbusReadCommand { get; }
    public ICommand ModbusWriteCommand { get; }


    public ICommand ModbusReadInputCommand { get; }


    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand SendCommand { get; }

    public bool CanConnect => !IsConnected;
    public bool CanDisconnect => IsConnected;
    public bool CanSend => IsConnected;

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            _isConnected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanConnect));
            OnPropertyChanged(nameof(CanDisconnect));
            OnPropertyChanged(nameof(CanSend));

           
            ((Command)ModbusReadCommand).ChangeCanExecute();
            ((Command)ModbusWriteCommand).ChangeCanExecute();
        }
    }

    public MainPageViewModel(ISerialService serialService)
    {
        _serialService = serialService;
        ConnectCommand = new Command(async () => await ConnectAsync());
        DisconnectCommand = new Command(async () => await DisconnectAsync());
        SendCommand = new Command(async () => await SendAsync());

        ModbusReadCommand = new Command(async () => await ModbusReadAsync(), () => IsConnected && SelectedSerialType == SerialType.Modbus);
        ModbusWriteCommand = new Command(async () => await ModbusWriteAsync(), () => IsConnected && SelectedSerialType == SerialType.Modbus);
        ModbusReadInputCommand = new Command(async () => await ModbusReadInputAsync(), () => IsConnected && SelectedSerialType == SerialType.Modbus);

        _ = InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        PortNames.Clear();
        var ports = await _serialService.GetDeviceNamesAsync();
        foreach (var port in ports)
            PortNames.Add(port);
        if (PortNames.Count > 0)
            SelectedPort = PortNames[0];
        SelectedParity = ParityOptions[0];
        SelectedStopBits = StopBitsOptions[0];
    }

    private async Task ConnectAsync()
    {
        int parityVal = ParityOptions.IndexOf(SelectedParity ?? "None");
        int stopBitsVal = StopBitsOptions.IndexOf(SelectedStopBits ?? "One");
        try
        {
            await _serialService.OpenAsync(SelectedPort, BaudRate, DataBits, stopBitsVal, parityVal, SelectedSerialType);
            IsConnected = true;
            StatusText = "상태: 연결됨";
            _readCts = new CancellationTokenSource();
            ReceivedText = "";
            _ = ReadLoopAsync(_readCts.Token);
        }
        catch (Exception ex)
        {
            StatusText = $"연결 오류: {ex.Message}";
        }
    }

    private async Task DisconnectAsync()
    {
        _readCts?.Cancel();
        await _serialService.CloseAsync();
        IsConnected = false;
        StatusText = "상태: 연결 안됨";
    }

    private async Task SendAsync()
    {
        if (string.IsNullOrEmpty(SendText)) return;
        var bytes = Encoding.UTF8.GetBytes(SendText);
        await _serialService.WriteAsync(bytes);
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var data = await _serialService.ReadAsync(ct);
            if (data != null && data.Length > 0)
            {
                ReceivedText += Encoding.UTF8.GetString(data);
            }
            await Task.Delay(100, ct);
        }
    }

    private async Task ModbusReadInputAsync()
    {
        if (SelectedSerialType != SerialType.Modbus) return;
        try
        {
            var result = await _serialService.ModbusReadInputRegistersAsync(ModbusSlaveId, ModbusAddress, ModbusQuantity);
            ReceivedText += $"[Modbus Read Input] {string.Join(", ", result)}\n";
        }
        catch (Exception ex)
        {
            StatusText = $"Modbus 읽기 오류: {ex.Message}";
        }
    }
    private async Task ModbusReadAsync()
    {
        if (SelectedSerialType != SerialType.Modbus) return;
        try
        {
            var result = await _serialService.ModbusReadHoldingRegistersAsync(ModbusSlaveId, ModbusAddress, ModbusQuantity);
            ReceivedText += $"[Modbus Read] {string.Join(", ", result)}\n";
        }
        catch (Exception ex)
        {
            StatusText = $"Modbus 읽기 오류: {ex.Message}";
        }
    }

    private async Task ModbusWriteAsync()
    {
        if (SelectedSerialType != SerialType.Modbus) return;
        try
        {
            
            await _serialService.ModbusWriteSingleRegisterAsync(ModbusSlaveId, ModbusAddress, ModbusQuantity);
            StatusText = "Modbus 쓰기 완료";
        }
        catch (Exception ex)
        {
            StatusText = $"Modbus 쓰기 오류: {ex.Message}";
        }
    }

}
