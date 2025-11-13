using MauiFirstUartApp.Core.Abstractions;
using MauiFirstUartApp.Core.Constants;

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
    public Color TypeColor => Type == LogType.Sent ? Color.FromArgb(SerialConstants.ColorSuccess) : Color.FromArgb(SerialConstants.ColorError);
}

public enum LogType
{
    Sent,
    Received
}

public class ActivityLogItem
{
    public string Time { get; set; } = "";
    public string Message { get; set; } = "";
    public Color BackgroundColor { get; set; }
    public Color MessageColor { get; set; }
}

public class ModbusDataItem
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Address { get; set; } = "";
    public string FunctionCode { get; set; } = "";
    public ushort Value { get; set; }
    public string HexValue => $"0x{Value:X4}";
    public string DataType { get; set; } = SerialConstants.DefaultDataType;
    public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss");
}

public class MainPageViewModel : BaseSerialViewModel
{
    private CancellationTokenSource? _readCts;

    #region Dashboard Properties

    // ActivityLog 컬렉션
    public ObservableCollection<ActivityLogItem> ActivityLog { get; } = new();

    // 대시보드용 통계 속성들  
    public int SentCount => SendCount;
    public int ReceivedCount => ReceiveCount;

    private int _errorCount;
    public int ErrorCount
    {
        get => _errorCount;
        set
        {
            _errorCount = value;
            OnPropertyChanged();
        }
    }

    // 연결 상태 관련 속성들
    public string ConnectionTypeText => SelectedSerialType == SerialType.Modbus ? "모드버스 RTU 통신" : "일반 시리얼 통신";
    public string PortName => SelectedPort ?? "선택 안됨";
    public string ModbusProtocol => SelectedModbusProtocol;
    public string SlaveId => ModbusSlaveId.ToString();

    // 모드 확인 속성들
    public bool IsSerialMode => SelectedSerialType == SerialType.Normal;
    public bool IsModbusMode => SelectedSerialType == SerialType.Modbus;

    // 상태 표시 색상 속성들
    public Color StatusBadgeColor => IsConnected ?
        (Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb(SerialConstants.ColorSuccess) : Color.FromArgb(SerialConstants.ColorSuccessBackground)) :
        (Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb(SerialConstants.ColorError) : Color.FromArgb(SerialConstants.ColorErrorBackground));

    public Color StatusBorderColor => IsConnected ? Color.FromArgb(SerialConstants.ColorSuccess) : Color.FromArgb(SerialConstants.ColorError);
    public Color StatusTextColor => IsConnected ? Color.FromArgb(SerialConstants.ColorSuccess) : Color.FromArgb(SerialConstants.ColorError);

    #endregion

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
            OnPropertyChanged(nameof(SentCount));
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
            OnPropertyChanged(nameof(ReceivedCount));
        }
    }

    public int TotalLogCount => SendCount + ReceiveCount;

    // 시리얼 전용 속성들
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

    public bool CanSend => IsConnected;

    // 명령어들
    public ICommand ClearMessageCommand { get; }
    public ICommand ClearLogsCommand { get; }
    public ICommand QuickSendCommand { get; }
    public ICommand SendCommand { get; }

    #endregion

    #region Modbus Properties

    // Modbus 데이터 컬렉션
    public ObservableCollection<ModbusDataItem> ModbusDataItems { get; } = new();

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
                OnPropertyChanged(nameof(ConnectionTypeText));
                OnPropertyChanged(nameof(IsSerialMode));
                OnPropertyChanged(nameof(IsModbusMode));

                ((Command)ModbusReadCommand).ChangeCanExecute();
                ((Command)ModbusWriteCommand).ChangeCanExecute();
                ((Command)ModbusReadInputCommand).ChangeCanExecute();
            }
        }
    }

    private byte _modbusSlaveId = SerialConstants.DefaultModbusSlaveId;
    public byte ModbusSlaveId
    {
        get => _modbusSlaveId;
        set { _modbusSlaveId = value; OnPropertyChanged(); OnPropertyChanged(nameof(SlaveId)); }
    }

    private ushort _modbusAddress = SerialConstants.DefaultModbusStartAddress;
    public ushort ModbusAddress
    {
        get => _modbusAddress;
        set { _modbusAddress = value; OnPropertyChanged(); }
    }

    private ushort _modbusQuantity = SerialConstants.DefaultModbusQuantity;
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
        set { _selectedModbusProtocol = value; OnPropertyChanged(); OnPropertyChanged(nameof(ModbusProtocol)); }
    }

    private int _modbusTimeout = SerialConstants.DefaultModbusTimeout;
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

    private string _selectedLanguage = SerialConstants.DefaultLanguage;
    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set { _selectedLanguage = value; OnPropertyChanged(); }
    }

    private bool _isDarkModeEnabled;
    public bool IsDarkModeEnabled
    {
        get => _isDarkModeEnabled;
        set
        {
            if (_isDarkModeEnabled != value)
            {
                _isDarkModeEnabled = value;
                OnPropertyChanged();
                ApplyTheme(value);
            }
        }
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

    private string _selectedLogRetention = SerialConstants.DefaultLogRetention;
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

    #region Commands

    public ICommand ModbusReadCommand { get; }
    public ICommand ModbusWriteCommand { get; }
    public ICommand ModbusReadInputCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand RefreshPortsCommand { get; }

    // Modbus 빠른 명령어들
    public ICommand ReadMultipleRegistersCommand { get; }
    public ICommand ReadCoilsCommand { get; }
    public ICommand WriteRegister100Command { get; }
    public ICommand WriteCoilOnCommand { get; }
    public ICommand ToggleAutoReadCommand { get; }
    public ICommand SelectReadTabCommand { get; }
    public ICommand SelectWriteTabCommand { get; }

    #endregion

    public MainPageViewModel(ISerialService serialService) : base(serialService)
    {
        // 기본 명령어들
        ConnectCommand = new Command(async () => await ConnectAsync());
        DisconnectCommand = new Command(async () => await DisconnectAsync());
        SendCommand = new Command(async () => await SendAsync());
        RefreshPortsCommand = new Command(async () => await RefreshPortsAsync());

        // 시리얼 터미널 명령어들
        ClearMessageCommand = new Command(() => SendText = "");
        ClearLogsCommand = new Command(() => ClearLogs());
        QuickSendCommand = new Command<string>(async (message) => await QuickSendAsync(message));

        // Modbus 명령어들
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
    }

    #region Overridden Methods

    /// <summary>
    /// 연결 상태 변경 시 추가 처리
    /// </summary>
    protected override void OnConnectionStatusChanged()
    {
        base.OnConnectionStatusChanged();

        OnPropertyChanged(nameof(CanSend));
        OnPropertyChanged(nameof(StatusBadgeColor));
        OnPropertyChanged(nameof(StatusBorderColor));
        OnPropertyChanged(nameof(StatusTextColor));

        // Modbus 명령어 상태 업데이트
        ((Command)ModbusReadCommand).ChangeCanExecute();
        ((Command)ModbusReadInputCommand).ChangeCanExecute();
        ((Command)ModbusWriteCommand).ChangeCanExecute();
    }

    #endregion

    #region Connection Methods

    private async Task ConnectAsync()
    {
        var success = await ConnectAsync(SelectedSerialType);
        if (success)
        {
            _readCts = new CancellationTokenSource();
            ReceivedText = "";

            if (SelectedSerialType != SerialType.Modbus)
            {
                _ = ReadLoopAsync(_readCts.Token);
            }
        }
    }

    private async Task DisconnectAsync()
    {
        _readCts?.Cancel();
        await base.DisconnectAsync();
    }

    private async Task RefreshPortsAsync()
    {
        await InitializeAsync();
    }

    #endregion

    #region Serial Communication Methods

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
            StatusText = string.Format(SerialConstants.StatusSendErrorFormat, ex.Message);
            ErrorCount++;
        }
    }

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
                StatusText = string.Format(SerialConstants.StatusReceiveErrorFormat, ex.Message);
                ErrorCount++;
            }

            await Task.Delay(SerialConstants.ReadLoopDelayMs, ct);
        }
    }

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

    #endregion

    #region Modbus Methods

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
                    FunctionCode = SerialConstants.FunctionCode03,
                    Value = result[i]
                };
                ModbusDataItems.Add(dataItem);
            }

            ModbusReadCount++;
            StatusText = SerialConstants.StatusModbusReadComplete;
        }
        catch (Exception ex)
        {
            StatusText = string.Format(SerialConstants.StatusModbusReadErrorFormat, ex.Message);
            ErrorCount++;
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
                    FunctionCode = SerialConstants.FunctionCode04,
                    Value = result[i]
                };
                ModbusDataItems.Add(dataItem);
            }

            ModbusReadCount++;
            StatusText = SerialConstants.StatusModbusInputReadComplete;
        }
        catch (Exception ex)
        {
            StatusText = string.Format(SerialConstants.StatusModbusReadErrorFormat, ex.Message);
            ErrorCount++;
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
                FunctionCode = SerialConstants.FunctionCode06,
                Value = WriteValue
            };
            ModbusDataItems.Add(dataItem);

            ModbusWriteCount++;
            StatusText = SerialConstants.StatusModbusWriteComplete;
        }
        catch (Exception ex)
        {
            StatusText = string.Format(SerialConstants.StatusModbusWriteErrorFormat, ex.Message);
            ErrorCount++;
        }
    }

    // Modbus 전용 메서드들
    private async Task ToggleAutoReadAsync()
    {
        if (IsAutoReadEnabled)
        {
            StatusText = "자동 읽기 시작";
        }
        else
        {
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

    #endregion

    #region Utility Methods

    private void ApplyTheme(bool isDarkMode)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Application.Current!.UserAppTheme = isDarkMode ? AppTheme.Dark : AppTheme.Light;
        });
    }

    #endregion
}
