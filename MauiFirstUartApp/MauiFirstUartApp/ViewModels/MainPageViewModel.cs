using MauiFirstUartApp.Core.Abstractions;
using MauiFirstUartApp.Core.Constants;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;



namespace MauiFirstUartApp.ViewModels;

public class SerialLogItem
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = SerialConstants.EmptyString;
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
    public string Time { get; set; } = SerialConstants.EmptyString;
    public string Message { get; set; } = SerialConstants.EmptyString;
    public Color BackgroundColor { get; set; }
    public Color MessageColor { get; set; }
}


public class ModbusDataItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public int Id { get; set; }

    private DateTime _timestamp;
    public DateTime Timestamp
    {
        get => _timestamp;
        set
        {
            if (_timestamp != value)
            {
                _timestamp = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedTimestamp)); // FormattedTimestamp도 갱신
            }
        }
    }

    public string Address { get; set; } = SerialConstants.EmptyString;
    public string FunctionCode { get; set; } = SerialConstants.EmptyString;

    private ushort _value;
    public ushort Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                HexValue = $"0x{_value:X4}"; // Value가 변경될 때 HexValue를 자동 계산
                OnPropertyChanged();
                OnPropertyChanged(nameof(HexValue));
            }
        }
    }

    public string HexValue { get; set; } = $"0x0000";

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
    public string ConnectionTypeText => SelectedSerialType == SerialType.Modbus ?
        SerialConstants.ModbusRtuCommunicationText :
        SerialConstants.SerialCommunicationText;

    public new string PortName => SelectedPort ?? SerialConstants.PortNotSelectedText;
    public string ModbusProtocol => SelectedModbusProtocol;
    public string SlaveId => ModbusSlaveId.ToString();

    // 모드 확인 속성들
    public bool IsSerialMode => SelectedSerialType == SerialType.Normal;
    public bool IsModbusMode => SelectedSerialType == SerialType.Modbus;

    // 상태 표시 색상 속성들
    public Color StatusBadgeColor => IsConnected ?
        (Application.Current?.RequestedTheme == AppTheme.Dark ?
            Color.FromArgb(SerialConstants.ColorSuccess) :
            Color.FromArgb(SerialConstants.ColorSuccessBackground)) :
        (Application.Current?.RequestedTheme == AppTheme.Dark ?
            Color.FromArgb(SerialConstants.ColorError) :
            Color.FromArgb(SerialConstants.ColorErrorBackground));

    public Color StatusBorderColor => IsConnected ?
        Color.FromArgb(SerialConstants.ColorSuccess) :
        Color.FromArgb(SerialConstants.ColorError);

    public Color StatusTextColor => IsConnected ?
        Color.FromArgb(SerialConstants.ColorSuccess) :
        Color.FromArgb(SerialConstants.ColorError);

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

    private string _receivedText = SerialConstants.EmptyString;
    public string ReceivedText
    {
        get => _receivedText;
        set { _receivedText = value; OnPropertyChanged(); }
    }

    public bool CanSend => IsConnected;

    public ICommand ClearMessageCommand { get; private set; }
    public ICommand ClearLogsCommand { get; private set; }
    public ICommand SendCommand { get; private set; }

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

                UpdateModbusCommandCanExecute();
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
        set
        {
            if (_isAutoReadEnabled != value)
            {
                _isAutoReadEnabled = value;
                OnPropertyChanged();
                _ = ToggleAutoReadAsync(); // 상태 변경 시 자동 읽기 실행/중지
            }
        }
    }


    private CancellationTokenSource? _autoSaveCts;

    public bool IsAutoSaveLogsEnabled
    {
        get => _isAutoSaveLogsEnabled;
        set
        {
            if (_isAutoSaveLogsEnabled != value)
            {
                _isAutoSaveLogsEnabled = value;
                OnPropertyChanged();

                if (_isAutoSaveLogsEnabled)
                {
                    // 자동 저장 시작
                    _autoSaveCts = new CancellationTokenSource();
                    _ = AutoSaveLogsAsync(_autoSaveCts.Token);
                }
                else
                {
                    // 자동 저장 중지
                    _autoSaveCts?.Cancel();
                }
            }
        }
    }

    private async Task AutoSaveLogsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // 로그 저장 경로
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ModbusLogs.txt");


                // 로그 데이터 생성
                var logBuilder = new StringBuilder();
                foreach (var log in SerialLogItems)
                {
                    logBuilder.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss} [{log.TypeLabel}] {log.Message}");
                }

                // 파일 저장
                File.WriteAllText(filePath, logBuilder.ToString());

                // 상태 업데이트
                StatusText = $"로그가 자동 저장되었습니다: {filePath}";
            }
            catch (Exception ex)
            {
                StatusText = $"로그 자동 저장 중 오류 발생: {ex.Message}";
            }

            // 1분 간격으로 저장
            await Task.Delay(TimeSpan.FromMinutes(10), ct);
        }
    }



    // 폴링 간격 옵션
    public ObservableCollection<string> PollingIntervalOptions { get; } = new() { "500ms", "1s", "2s", "5s" };

    private string _selectedPollingInterval = SerialConstants.DefaultPollingIntervalText;
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

    private string _selectedReadFunction = SerialConstants.DefaultReadFunctionText;
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

    private string _selectedWriteFunction = SerialConstants.DefaultWriteFunctionText;
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

    // Baud Rate 선택 관련 속성 추가
    public ObservableCollection<string> BaudRateOptions { get; } = new() { "9600", "15500", "19200", "38400", "57600", "115200" };

    private string _selectedBaudRate = "115200";
    public string SelectedBaudRate
    {
        get => _selectedBaudRate;
        set
        {
            _selectedBaudRate = value;
            OnPropertyChanged();
            // BaseSerialViewModel의 BaudRate 속성도 업데이트
            if (int.TryParse(value, out int baudRateInt))
            {
                BaudRate = baudRateInt;
            }
        }
    }

    // Modbus 프로토콜 관련
    public ObservableCollection<string> ModbusProtocolOptions { get; } = new() { "Modbus RTU", "Modbus ASCII" };

    private string _selectedModbusProtocol = SerialConstants.DefaultModbusProtocolText;
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
    public ObservableCollection<string> LanguageOptions { get; } = new() { "한국어", "English", "日본語" };

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
        set
        {
            if (_selectedLogRetention != value)
            {
                _selectedLogRetention = value;
                OnPropertyChanged();
                ApplyLogRetentionPolicy();
            }
        }
    }


    private void ApplyLogRetentionPolicy()
    {
        try
        {
            var filePath = Path.Combine(FileSystem.AppDataDirectory, "ModbusLogs.txt");

            if (File.Exists(filePath))
            {
                var retentionDays = SelectedLogRetention switch
                {
                    "1일" => 1,
                    "7일" => 7,
                    "30일" => 30,
                    "90일" => 90,
                    _ => 30 // 기본값
                };

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-retentionDays))
                {
                    File.Delete(filePath);
                    StatusText = "오래된 로그 파일이 삭제되었습니다.";
                }
            }
        }
        catch (Exception ex)
        {
            StatusText = $"로그 보관 정책 적용 중 오류 발생: {ex.Message}";
        }
    }

    private bool _isAutoSaveLogsEnabled = true;


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

    // MainPageViewModel.cs의 Modbus Properties 섹션에 추가
    public bool IsNormalModeSelected
    {
        get => SelectedSerialType == SerialType.Normal;
        set
        {
            if (value)
            {
                SelectedSerialType = SerialType.Normal;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsModbusModeSelected));
            }
        }
    }

    public bool IsModbusModeSelected
    {
        get => SelectedSerialType == SerialType.Modbus;
        set
        {
            if (value)
            {
                SelectedSerialType = SerialType.Modbus;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNormalModeSelected));
            }
        }
    }



    #endregion

    #region Commands

    // 모든 Command 속성들을 private set으로 변경
    public ICommand ModbusReadCommand { get; private set; }
    public ICommand ModbusWriteCommand { get; private set; }
    public ICommand ModbusReadInputCommand { get; private set; }
    public ICommand ConnectCommand { get; private set; }
    public ICommand DisconnectCommand { get; private set; }
    public ICommand RefreshPortsCommand { get; private set; }

    public ICommand QuickSendCommand { get; private set; } // 추가

    // Modbus 빠른 명령어들
    public ICommand ReadMultipleRegistersCommand { get; private set; }
    public ICommand ReadCoilsCommand { get; private set; }
    public ICommand WriteRegister100Command { get; private set; }
    public ICommand WriteCoilOnCommand { get; private set; }
    public ICommand ToggleAutoReadCommand { get; private set; }
    public ICommand SelectReadTabCommand { get; private set; }
    public ICommand SelectWriteTabCommand { get; private set; }
    public ICommand ExportToCsvCommand { get; private set; }
    #endregion

    public MainPageViewModel(ISerialService serialService) : base(serialService)
    {
        InitializeCommands();
    }

    #region Initialization

    /// <summary>
    /// 모든 명령어 초기화
    /// </summary>
    private void InitializeCommands()
    {
        // 기본 명령어들
        ConnectCommand = new Command(async () => await ConnectAsync());
        DisconnectCommand = new Command(async () => await DisconnectAsync());
        SendCommand = new Command(async () => await SendAsync());
        RefreshPortsCommand = new Command(async () => await RefreshPortsAsync());

        //CSV 내보내기 명령어
        ExportToCsvCommand = new Command(ExportToCsv);

        // 시리얼 터미널 명령어들
        ClearMessageCommand = new Command(() => SendText = SerialConstants.EmptyString);
        ClearLogsCommand = new Command(() => ClearLogs());

        // Modbus 명령어들
        ModbusReadCommand = new Command(
            async () => await ExecuteWithErrorHandling(ModbusReadAsync),
            () => IsConnected && SelectedSerialType == SerialType.Modbus);

        ModbusWriteCommand = new Command(
            async () => await ExecuteWithErrorHandling(ModbusWriteAsync),
            () => IsConnected && SelectedSerialType == SerialType.Modbus);

        ModbusReadInputCommand = new Command(
            async () => await ExecuteWithErrorHandling(ModbusReadInputAsync),
            () => IsConnected && SelectedSerialType == SerialType.Modbus);

        // Modbus 전용 명령어들
        ClearModbusDataCommand = new Command(() => ClearModbusData());
        SelectReadTabCommand = new Command(() => IsReadTabSelected = true);
        SelectWriteTabCommand = new Command(() => IsReadTabSelected = false);
        ToggleAutoReadCommand = new Command(async () => await ToggleAutoReadAsync());
        ReadMultipleRegistersCommand = new Command(async () => await ExecuteWithErrorHandling(ReadMultipleRegistersAsync));
        ReadCoilsCommand = new Command(async () => await ReadCoilsAsync());
        WriteRegister100Command = new Command(async () => await ExecuteWithErrorHandling(WriteRegisterAsync));
        WriteCoilOnCommand = new Command(async () => await WriteCoilOnAsync());
        QuickSendCommand = new Command<string>((message) => { /* 사용하지 않음 */ });
    }

    #endregion

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

        UpdateModbusCommandCanExecute();
    }

    #endregion

    #region Connection Methods

    private async Task ConnectAsync()
    {
        var success = await ConnectAsync(SelectedSerialType);
        if (success)
        {
            _readCts = new CancellationTokenSource();
            ReceivedText = SerialConstants.EmptyString;

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

            AddSerialLogItem(SendText, LogType.Sent);
            SendCount++;

            // 일반 시리얼 모드에서는 전송 후 텍스트 지우기
            if (SelectedSerialType == SerialType.Normal)
            {
                SendText = SerialConstants.EmptyString;
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

                    ProcessReceivedData(buffer);
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

    private void ProcessReceivedData(StringBuilder buffer)
    {
        // 줄바꿈 문자가 있으면 로그에 추가
        var lines = buffer.ToString().Split('\n');
        for (int i = 0; i < lines.Length - 1; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                AddSerialLogItem(lines[i].Trim(), LogType.Received);
                ReceiveCount++;
            }
        }

        // 마지막 불완전한 줄은 버퍼에 유지
        buffer.Clear();
        buffer.Append(lines[^1]);
    }

    private void AddSerialLogItem(string message, LogType type)
    {
        var logItem = new SerialLogItem
        {
            Timestamp = DateTime.Now,
            Message = IsTimestampDisplayEnabled ? message : message,
            Type = type
        };
        SerialLogItems.Add(logItem);
    }


    private void ClearLogs()
    {
        SerialLogItems.Clear();
        SendCount = 0;
        ReceiveCount = 0;
    }

    #endregion

    #region Modbus Methods

    private void ExportToCsv()
    {
        try
        {
            // CSV 파일 경로 설정
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ModbusData.csv");

            // CSV 데이터 생성
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Id,Timestamp,Address,FunctionCode,Value,HexValue,DataType");

            foreach (var item in ModbusDataItems)
            {
                csvBuilder.AppendLine($"{item.Id},{item.Timestamp:yyyy-MM-dd HH:mm:ss},{item.Address},{item.FunctionCode},{item.Value},{item.HexValue},{item.DataType}");
            }

            // 파일 저장
            File.WriteAllText(filePath, csvBuilder.ToString());

            // 사용자에게 알림
            StatusText = $"CSV 파일이 저장되었습니다: {filePath}";
        }
        catch (Exception ex)
        {
            StatusText = $"CSV 저장 중 오류 발생: {ex.Message}";
        }
    }


    private async Task ModbusReadAsync()
    {
        // 읽기 실행 전 이전 데이터 클리어
        ModbusDataItems.Clear();

        var result = await _serialService.ModbusReadHoldingRegistersAsync(ModbusSlaveId, ModbusAddress, ModbusQuantity);
        AddModbusDataItems(result, SerialConstants.FunctionCode03);
        ModbusReadCount++;
        StatusText = SerialConstants.StatusModbusReadComplete;
    }

    private async Task ModbusReadInputAsync()
    {
        // 읽기 실행 전 이전 데이터 클리어
        ModbusDataItems.Clear();

        var result = await _serialService.ModbusReadInputRegistersAsync(ModbusSlaveId, ModbusAddress, ModbusQuantity);
        AddModbusDataItems(result, SerialConstants.FunctionCode04);
        ModbusReadCount++;
        StatusText = SerialConstants.StatusModbusInputReadComplete;
    }

    private async Task ModbusWriteAsync()
    {
        ModbusDataItems.Clear();

        await _serialService.ModbusWriteSingleRegisterAsync(ModbusSlaveId, ModbusAddress, WriteValue);

        var readResult = await _serialService.ModbusReadHoldingRegistersAsync(ModbusSlaveId, ModbusAddress, ModbusQuantity);

        AddModbusDataItems(readResult, SerialConstants.FunctionCode03);

        ModbusWriteCount++;
        ModbusReadCount++; // 쓰기 후 자동 읽기도 카운트

        StatusText = $"쓰기 완료 후 {ModbusQuantity}개 레지스터 읽기 완료";
    }

    public ICommand ClearModbusDataCommand { get; private set; }

    private void AddModbusDataItems(ushort[] values, string functionCode)
    {
        for (int i = 0; i < values.Length; i++)
        {
            var address = (ModbusAddress + i).ToString();

            var existingItem = ModbusDataItems.FirstOrDefault(item => item.Address == address);

            if (existingItem != null)
            {
                existingItem.Value = values[i];
                existingItem.Timestamp = DateTime.Now;
            }
            else
            {
                var dataItem = new ModbusDataItem
                {
                    Id = ModbusDataItems.Count + 1,
                    Timestamp = DateTime.Now,
                    Address = address,
                    FunctionCode = functionCode,
                    Value = values[i],
                    HexValue = IsHexFormatEnabled ? $"0x{values[i]:X4}" : values[i].ToString()
                };
                ModbusDataItems.Add(dataItem);
            }
        }
    }




    // Modbus 전용 메서드들
    private void ClearModbusData()
    {
        ModbusDataItems.Clear();
        StatusText = "데이터 테이블이 지워졌습니다.";
    }

    private async Task ToggleAutoReadAsync()
    {
        if (IsAutoReadEnabled)
        {
            // 자동 읽기 시작
            _readCts = new CancellationTokenSource();
            StatusText = SerialConstants.AutoReadStartMessage;

            await StartAutoReadAsync(_readCts.Token);
        }
        else
        {
            // 자동 읽기 중지
            _readCts?.Cancel();
            StatusText = SerialConstants.AutoReadStopMessage;
        }
    }
    private async Task StartAutoReadAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Modbus 데이터 읽기
                var result = await _serialService.ModbusReadHoldingRegistersAsync(ModbusSlaveId, ModbusAddress, ModbusQuantity);
                AddModbusDataItems(result, SerialConstants.FunctionCode03);

                ModbusReadCount++;
                StatusText = $"마지막 읽기: {DateTime.Now:HH:mm:ss}"; // 읽기 시간 갱신
            }
            catch (OperationCanceledException)
            {
                break; // 작업이 취소된 경우 루프 종료
            }
            catch (Exception ex)
            {
                StatusText = string.Format(SerialConstants.StatusModbusReadErrorFormat, ex.Message);
                ErrorCount++;
            }

            // 폴링 간격 대기
            if (int.TryParse(SelectedPollingInterval.Replace("ms", "").Replace("s", "000"), out int delay))
            {
                await Task.Delay(delay, ct);
            }
        }
    }

    private async Task ReadMultipleRegistersAsync()
    {
        await ModbusReadAsync();
    }

    private async Task ReadCoilsAsync()
    {
        StatusText = SerialConstants.CoilReadNotImplementedMessage;
    }

    private async Task WriteRegisterAsync()
    {
        // 현재 사용자가 설정한 값들을 그대로 사용
        await ModbusWriteAsync();
    }

    private async Task WriteCoilOnAsync()
    {
        StatusText = SerialConstants.CoilWriteNotImplementedMessage;
    }

    #endregion


    #region Error Handling

    /// <summary>
    /// 공통 오류 처리가 적용된 메서드 실행
    /// </summary>
    private async Task ExecuteWithErrorHandling(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            StatusText = string.Format(SerialConstants.StatusModbusReadErrorFormat, ex.Message);
            ErrorCount++;
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Modbus 명령어들의 CanExecute 상태 업데이트
    /// </summary>
    private void UpdateModbusCommandCanExecute()
    {
        ((Command)ModbusReadCommand).ChangeCanExecute();
        ((Command)ModbusReadInputCommand).ChangeCanExecute();
        ((Command)ModbusWriteCommand).ChangeCanExecute();
    }

    private void ApplyTheme(bool isDarkMode)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Application.Current!.UserAppTheme = isDarkMode ? AppTheme.Dark : AppTheme.Light;
        });
    }

    #endregion
}
