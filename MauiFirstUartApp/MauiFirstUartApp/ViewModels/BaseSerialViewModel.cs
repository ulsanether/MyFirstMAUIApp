// ViewModels/BaseSerialViewModel.cs
using MauiFirstUartApp.Core.Abstractions;
using MauiFirstUartApp.Core.Constants;

using System.Collections.ObjectModel;

namespace MauiFirstUartApp.ViewModels;

public abstract class BaseSerialViewModel : BindableObject
{
    protected readonly ISerialService _serialService;

    public ObservableCollection<string> PortNames { get; } = new();
    public ObservableCollection<string> ParityOptions { get; } = new(Enum.GetNames(typeof(SerialParity)));
    public ObservableCollection<string> StopBitsOptions { get; } = new(Enum.GetNames(typeof(SerialStopBits)));

    private string? _selectedPort;
    public string SelectedPort
    {
        get => _selectedPort;
        set
        {
            if (_selectedPort != value)
            {
                _selectedPort = value;
                OnPropertyChanged(nameof(SelectedPort));
                OnPropertyChanged(nameof(PortName)); // PortName 변경 알림 추가
            }
        }
    }

    public virtual string PortName => SelectedPort ?? "포트가 선택되지 않음";

    private string? _selectedParity;
    public string? SelectedParity
    {
        get => _selectedParity;
        set { _selectedParity = value; OnPropertyChanged(); }
    }

    private string _selectedStopBits = SerialConstants.DefaultStopBits;
    public string SelectedStopBits
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

    private string _statusText = "상태: 연결 안됨";
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

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
            OnConnectionStatusChanged();
        }
    }

    public bool CanConnect => !IsConnected;
    public bool CanDisconnect => IsConnected;

    protected BaseSerialViewModel(ISerialService serialService)
    {
        _serialService = serialService;
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

    protected virtual void OnConnectionStatusChanged() { }

    protected async Task<bool> ConnectAsync(SerialType serialType)
    {
        int parityVal = ParityOptions.IndexOf(SelectedParity ?? "None");
        int stopBitsVal = StopBitsOptions.IndexOf(SelectedStopBits ?? "One");
        try
        {
            await _serialService.OpenAsync(SelectedPort, BaudRate, DataBits, stopBitsVal, parityVal, serialType);
            IsConnected = true;
            StatusText = "상태: 연결됨";
            return true;
        }
        catch (Exception ex)
        {
            StatusText = $"연결 오류: {ex.Message}";
            return false;
        }
    }

    protected async Task DisconnectAsync()
    {
        await _serialService.CloseAsync();
        IsConnected = false;
        StatusText = "상태: 연결 안됨";
    }
}
