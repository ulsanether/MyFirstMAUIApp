using MauiFirstUartApp.Core.Abstractions;

using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;

namespace MauiFirstUartApp.ViewModels;


public class SerialLogItem
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = "";
    public LogType Type { get; set; }
    public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss");
    public string TypeLabel => Type == LogType.Sent ? "송신" : "수신";
    public Color TypeColor => Type == LogType.Sent ? Color.FromArgb("#059669") : Color.FromArgb("#DC2626");
}

public enum LogType
{
    Sent,
    Received
}



public class ModbusDataItem
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Address { get; set; } = "";
    public string FunctionCode { get; set; } = "";
    public ushort Value { get; set; }
    public string HexValue => $"0x{Value:X4}";
    public string DataType { get; set; } = "UINT16";
    public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss");
}

public class MainPageViewModel : BindableObject
{



    private readonly ISerialService _serialService;
    private CancellationTokenSource? _readCts;

    public ObservableCollection<string> PortNames { get; } = new();
    public ObservableCollection<string> ParityOptions { get; } = new(Enum.GetNames(typeof(SerialParity)));
    public ObservableCollection<string> StopBitsOptions { get; } = new(Enum.GetNames(typeof(SerialStopBits)));


    #region Serial Terminal Properties

    // 시리얼 로그 데이터 컬렉션
    public ObservableCollection<SerialLogItem> SerialLogItems { get; } = new();

    // 통계
    private int _sendCount;
    public int SendCount
    {
        get => _sendCount;
        set
        {
            _sendCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TotalLogCount));
        }
    }

    private int _receiveCount;
    public int ReceiveCount
    {
        get => _receiveCount;
        set
        {
            _receiveCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TotalLogCount));
        }
    }

    public int TotalLogCount => SendCount + ReceiveCount;

    // 명령어들
    public ICommand ClearMessageCommand { get; }
    public ICommand ClearLogsCommand { get; }
    public ICommand QuickSendCommand { get; }

    #endregion


    #region Modbus Properties

    // Modbus 데이터 컬렉션
    public ObservableCollection<ModbusDataItem> ModbusDataItems { get; } = new();

    private byte _modbusSlaveId = 1;
    public byte ModbusSlaveId
    {
        get => _modbusSlaveId;
        set { _modbusSlaveId = value; OnPropertyChanged(); }
    }

    private ushort _modbusAddress = 0;
    public ushort ModbusAddress
    {
        get => _modbusAddress;
        set { _modbusAddress = value; OnPropertyChanged(); }
    }

    private ushort _modbusQuantity = 1;
    public ushort ModbusQuantity
    {
        get => _modbusQuantity;
        set { _modbusQuantity = value; OnPropertyChanged(); }
    }

    // 자동 읽기 기능
    private bool _isAutoReadEnabled;
    public bool IsAutoReadEnabled
    {
        get => _isAutoReadEnabled;
        set { _isAutoReadEnabled = value; OnPropertyChanged(); }
    }

    // 폴링 간격 옵션
    public ObservableCollection<string> PollingIntervalOptions { get; } = new() { "500ms", "1s", "2s", "5s" };

    private string _selectedPollingInterval = "1s";
    public string SelectedPollingInterval
    {
        get => _selectedPollingInterval;
        set { _selectedPollingInterval = value; OnPropertyChanged(); }
    }

    // 읽기 기능 코드 옵션
    public ObservableCollection<string> ReadFunctionOptions { get; } = new()
    {
        "03 - Read Holding Registers",
        "04 - Read Input Registers"
    };

    private string _selectedReadFunction = "03 - Read Holding Registers";
    public string SelectedReadFunction
    {
        get => _selectedReadFunction;
        set { _selectedReadFunction = value; OnPropertyChanged(); }
    }

    // 쓰기 기능 코드 옵션
    public ObservableCollection<string> WriteFunctionOptions { get; } = new()
    {
        "06 - Write Single Register",
        "16 - Write Multiple Registers"
    };

    private string _selectedWriteFunction = "06 - Write Single Register";
    public string SelectedWriteFunction
    {
        get => _selectedWriteFunction;
        set { _selectedWriteFunction = value; OnPropertyChanged(); }
    }

    // 읽기/쓰기 값
    private ushort _writeValue;
    public ushort WriteValue
    {
        get => _writeValue;
        set { _writeValue = value; OnPropertyChanged(); }
    }

    // 통계
    private int _modbusReadCount;
    public int ModbusReadCount
    {
        get => _modbusReadCount;
        set
        {
            _modbusReadCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ModbusTotalCount));
        }
    }

    private int _modbusWriteCount;
    public int ModbusWriteCount
    {
        get => _modbusWriteCount;
        set
        {
            _modbusWriteCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ModbusTotalCount));
        }
    }

    public int ModbusTotalCount => ModbusReadCount + ModbusWriteCount;

    // 탭 관리
    private bool _isReadTabSelected = true;
    public bool IsReadTabSelected
    {
        get => _isReadTabSelected;
        set
        {
            _isReadTabSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsWriteTabSelected));
        }
    }

    public bool IsWriteTabSelected => !_isReadTabSelected;

    #endregion

    #region Settings Properties

    // Modbus 프로토콜 관련
    public ObservableCollection<string> ModbusProtocolOptions { get; } = new() { "Modbus RTU", "Modbus ASCII" };

    private string _selectedModbusProtocol = "Modbus RTU";
    public string SelectedModbusProtocol
    {
        get => _selectedModbusProtocol;
        set { _selectedModbusProtocol = value; OnPropertyChanged(); }
    }

    private int _modbusTimeout = 1000;
    public int ModbusTimeout
    {
        get => _modbusTimeout;
        set { _modbusTimeout = value; OnPropertyChanged(); }
    }

    private bool _isAutoReconnectEnabled = true;
    public bool IsAutoReconnectEnabled
    {
        get => _isAutoReconnectEnabled;
        set { _isAutoReconnectEnabled = value; OnPropertyChanged(); }
    }

    private bool _isCrcVerificationEnabled = true;
    public bool IsCrcVerificationEnabled
    {
        get => _isCrcVerificationEnabled;
        set { _isCrcVerificationEnabled = value; OnPropertyChanged(); }
    }

    private bool _isRtsEnabled;
    public bool IsRtsEnabled
    {
        get => _isRtsEnabled;
        set { _isRtsEnabled = value; OnPropertyChanged(); }
    }

    private bool _isDtrEnabled;
    public bool IsDtrEnabled
    {
        get => _isDtrEnabled;
        set { _isDtrEnabled = value; OnPropertyChanged(); }
    }

    // 일반 설정
    public ObservableCollection<string> LanguageOptions { get; } = new() { "한국어", "English", "日本語" };

    private string _selectedLanguage = "한국어";
    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set { _selectedLanguage = value; OnPropertyChanged(); }
    }

    private bool _isDarkModeEnabled;
    public bool IsDarkModeEnabled
    {
        get => _isDarkModeEnabled;
        set { _isDarkModeEnabled = value; OnPropertyChanged(); }
    }

    // 알림 설정
    private bool _isConnectionNotificationEnabled = true;
    public bool IsConnectionNotificationEnabled
    {
        get => _isConnectionNotificationEnabled;
        set { _isConnectionNotificationEnabled = value; OnPropertyChanged(); }
    }

    private bool _isErrorNotificationEnabled = true;
    public bool IsErrorNotificationEnabled
    {
        get => _isErrorNotificationEnabled;
        set { _isErrorNotificationEnabled = value; OnPropertyChanged(); }
    }

    private bool _isSoundNotificationEnabled;
    public bool IsSoundNotificationEnabled
    {
        get => _isSoundNotificationEnabled;
        set { _isSoundNotificationEnabled = value; OnPropertyChanged(); }
    }

    // 데이터 설정
    public ObservableCollection<string> LogRetentionOptions { get; } = new() { "1일", "7일", "30일", "90일" };

    private string _selectedLogRetention = "7일";
    public string SelectedLogRetention
    {
        get => _selectedLogRetention;
        set { _selectedLogRetention = value; OnPropertyChanged(); }
    }

    private bool _isAutoSaveLogsEnabled = true;
    public bool IsAutoSaveLogsEnabled
    {
        get => _isAutoSaveLogsEnabled;
        set { _isAutoSaveLogsEnabled = value; OnPropertyChanged(); }
    }

    private bool _isTimestampDisplayEnabled = true;
    public bool IsTimestampDisplayEnabled
    {
        get => _isTimestampDisplayEnabled;
        set { _isTimestampDisplayEnabled = value; OnPropertyChanged(); }
    }

    private bool _isHexFormatEnabled;
    public bool IsHexFormatEnabled
    {
        get => _isHexFormatEnabled;
        set { _isHexFormatEnabled = value; OnPropertyChanged(); }
    }

    #endregion

    #region Serial Properties

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
            ((Command)ModbusReadInputCommand).ChangeCanExecute();
            ((Command)ModbusWriteCommand).ChangeCanExecute();
        }
    }

    public bool CanConnect => !IsConnected;
    public bool CanDisconnect => IsConnected;
    public bool CanSend => IsConnected;

    #endregion

    #region Commands

    public ICommand ModbusReadCommand { get; }
    public ICommand ModbusWriteCommand { get; }
    public ICommand ModbusReadInputCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand SendCommand { get; }

    // Modbus 빠른 명령어들
    public ICommand ReadMultipleRegistersCommand { get; }
    public ICommand ReadCoilsCommand { get; }
    public ICommand WriteRegister100Command { get; }
    public ICommand WriteCoilOnCommand { get; }
    public ICommand ToggleAutoReadCommand { get; }
    public ICommand SelectReadTabCommand { get; }
    public ICommand SelectWriteTabCommand { get; }

    #endregion

    public MainPageViewModel(ISerialService serialService)
    {
        _serialService = serialService;

        // 기본 명령어들
        ConnectCommand = new Command(async () => await ConnectAsync());
        DisconnectCommand = new Command(async () => await DisconnectAsync());
        SendCommand = new Command(async () => await SendAsync());

        // 시리얼 터미널 명령어들 추가
        ClearMessageCommand = new Command(() => SendText = "");
        ClearLogsCommand = new Command(() => ClearLogs());
        QuickSendCommand = new Command<string>(async (message) => await QuickSendAsync(message));



        ModbusReadCommand = new Command(async () => await ModbusReadAsync(), () => IsConnected && SelectedSerialType == SerialType.Modbus);
        ModbusWriteCommand = new Command(async () => await ModbusWriteAsync(), () => IsConnected && SelectedSerialType == SerialType.Modbus);
        ModbusReadInputCommand = new Command(async () => await ModbusReadInputAsync(), () => IsConnected && SelectedSerialType == SerialType.Modbus);

        // Modbus 전용 명령어들
        SelectReadTabCommand = new Command(() => IsReadTabSelected = true);
        SelectWriteTabCommand = new Command(() => IsReadTabSelected = false);
        ToggleAutoReadCommand = new Command(async () => await ToggleAutoReadAsync());
        ReadMultipleRegistersCommand = new Command(async () => await ReadMultipleRegistersAsync());
        ReadCoilsCommand = new Command(async () => await ReadCoilsAsync());
        WriteRegister100Command = new Command(async () => await WriteRegister100Async());
        WriteCoilOnCommand = new Command(async () => await WriteCoilOnAsync());

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

            if (SelectedSerialType != SerialType.Modbus)
            {
                _ = ReadLoopAsync(_readCts.Token);
            }
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

    // 기존 SendAsync 메서드 수정
    private async Task SendAsync()
    {
        if (string.IsNullOrEmpty(SendText)) return;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(SendText);
            await _serialService.WriteAsync(bytes);

            // 로그에 추가
            var logItem = new SerialLogItem
            {
                Timestamp = DateTime.Now,
                Message = SendText,
                Type = LogType.Sent
            };
            SerialLogItems.Add(logItem);
            SendCount++;

            // 일반 시리얼 모드에서는 전송 후 텍스트 지우기
            if (SelectedSerialType == SerialType.Normal)
            {
                SendText = "";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"전송 오류: {ex.Message}";
        }
    }

    // 기존 ReadLoopAsync 메서드 수정
    private async Task ReadLoopAsync(CancellationToken ct)
    {
        var buffer = new StringBuilder();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var data = await _serialService.ReadAsync(ct);
                if (data != null && data.Length > 0)
                {
                    var receivedText = Encoding.UTF8.GetString(data);
                    buffer.Append(receivedText);

                    // 줄바꿈 문자가 있으면 로그에 추가
                    var lines = buffer.ToString().Split('\n');
                    for (int i = 0; i < lines.Length - 1; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(lines[i]))
                        {
                            var logItem = new SerialLogItem
                            {
                                Timestamp = DateTime.Now,
                                Message = lines[i].Trim(),
                                Type = LogType.Received
                            };
                            SerialLogItems.Add(logItem);
                            ReceiveCount++;
                        }
                    }

                    // 마지막 불완전한 줄은 버퍼에 유지
                    buffer.Clear();
                    buffer.Append(lines[^1]);

                    // 전체 수신 텍스트도 업데이트 (기존 동작 유지)
                    ReceivedText += receivedText;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                StatusText = $"수신 오류: {ex.Message}";
            }

            await Task.Delay(100, ct);
        }
    }

    // 시리얼 터미널 메서드들
    private void ClearLogs()
    {
        SerialLogItems.Clear();
        SendCount = 0;
        ReceiveCount = 0;
    }

    private async Task QuickSendAsync(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        var originalText = SendText;
        SendText = message;
        await SendAsync();
        SendText = originalText;
    }




    private async Task ModbusReadAsync()
    {
        try
        {
            var result = await _serialService.ModbusReadHoldingRegistersAsync(ModbusSlaveId, ModbusAddress, ModbusQuantity);

            // 결과를 데이터 테이블에 추가
            for (int i = 0; i < result.Length; i++)
            {
                var dataItem = new ModbusDataItem
                {
                    Id = ModbusDataItems.Count + 1,
                    Timestamp = DateTime.Now,
                    Address = (ModbusAddress + i).ToString(),
                    FunctionCode = "03",
                    Value = result[i],
                    DataType = "UINT16"
                };
                ModbusDataItems.Add(dataItem);
            }

            ModbusReadCount++;
            StatusText = "Modbus 읽기 완료";
        }
        catch (Exception ex)
        {
            StatusText = $"Modbus 읽기 오류: {ex.Message}";
        }
    }

    private async Task ModbusReadInputAsync()
    {
        try
        {
            var result = await _serialService.ModbusReadInputRegistersAsync(ModbusSlaveId, ModbusAddress, ModbusQuantity);

            // 결과를 데이터 테이블에 추가
            for (int i = 0; i < result.Length; i++)
            {
                var dataItem = new ModbusDataItem
                {
                    Id = ModbusDataItems.Count + 1,
                    Timestamp = DateTime.Now,
                    Address = (ModbusAddress + i).ToString(),
                    FunctionCode = "04",
                    Value = result[i],
                    DataType = "UINT16"
                };
                ModbusDataItems.Add(dataItem);
            }

            ModbusReadCount++;
            StatusText = "Modbus 입력 레지스터 읽기 완료";
        }
        catch (Exception ex)
        {
            StatusText = $"Modbus 읽기 오류: {ex.Message}";
        }
    }

    private async Task ModbusWriteAsync()
    {
        try
        {
            await _serialService.ModbusWriteSingleRegisterAsync(ModbusSlaveId, ModbusAddress, WriteValue);

            // 쓰기 결과를 데이터 테이블에 추가
            var dataItem = new ModbusDataItem
            {
                Id = ModbusDataItems.Count + 1,
                Timestamp = DateTime.Now,
                Address = ModbusAddress.ToString(),
                FunctionCode = "06",
                Value = WriteValue,
                DataType = "UINT16"
            };
            ModbusDataItems.Add(dataItem);

            ModbusWriteCount++;
            StatusText = "Modbus 쓰기 완료";
        }
        catch (Exception ex)
        {
            StatusText = $"Modbus 쓰기 오류: {ex.Message}";
        }
    }

    // Modbus 전용 메서드들
    private async Task ToggleAutoReadAsync()
    {
        if (IsAutoReadEnabled)
        {
            // 자동 읽기 시작
            StatusText = "자동 읽기 시작";
        }
        else
        {
            // 자동 읽기 중지
            StatusText = "자동 읽기 중지";
        }
    }

    private async Task ReadMultipleRegistersAsync()
    {
        var originalAddress = ModbusAddress;
        var originalQuantity = ModbusQuantity;

        ModbusAddress = 0;
        ModbusQuantity = 10;

        await ModbusReadAsync();

        ModbusAddress = originalAddress;
        ModbusQuantity = originalQuantity;
    }

    private async Task ReadCoilsAsync()
    {
        StatusText = "코일 읽기 기능은 아직 구현되지 않았습니다.";
    }

    private async Task WriteRegister100Async()
    {
        var originalAddress = ModbusAddress;
        var originalValue = WriteValue;

        ModbusAddress = 0;
        WriteValue = 100;

        await ModbusWriteAsync();

        ModbusAddress = originalAddress;
        WriteValue = originalValue;
    }

    private async Task WriteCoilOnAsync()
    {
        StatusText = "코일 쓰기 기능은 아직 구현되지 않았습니다.";
    }
}

