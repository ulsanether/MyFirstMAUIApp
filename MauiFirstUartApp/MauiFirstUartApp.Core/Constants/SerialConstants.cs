namespace MauiFirstUartApp.Core.Constants;

/// <summary>
/// 시리얼 통신 및 Modbus 관련 상수 정의
/// </summary>
public static class SerialConstants
{
    #region Serial Communication Constants

    /// <summary>
    /// 기본 보드레이트
    /// </summary>
    public const int DefaultBaudRate = 115200;

    /// <summary>
    /// 기본 데이터 비트 수
    /// </summary>
    public const int DefaultDataBits = 8;

    /// <summary>
    /// 시리얼 포트 읽기 타임아웃 (밀리초)
    /// </summary>
    public const int SerialReadTimeout = 3000;

    /// <summary>
    /// 시리얼 포트 쓰기 타임아웃 (밀리초)
    /// </summary>
    public const int SerialWriteTimeout = 3000;

    /// <summary>
    /// 읽기 버퍼 크기
    /// </summary>
    public const int ReadBufferSize = 4096;

    /// <summary>
    /// 읽기 루프 지연 시간 (밀리초)
    /// </summary>
    public const int ReadLoopDelayMs = 100;

    #endregion

    #region Modbus Constants

    /// <summary>
    /// Modbus 최대 레지스터 읽기/쓰기 포인트 수
    /// </summary>
    public const ushort ModbusMaxPoints = 125;

    /// <summary>
    /// Modbus 최소 레지스터 읽기/쓰기 포인트 수
    /// </summary>
    public const ushort ModbusMinPoints = 1;

    /// <summary>
    /// 기본 Modbus 폴링 간격 (밀리초)
    /// </summary>
    public const int DefaultModbusPollingInterval = 1000;

    /// <summary>
    /// 기본 Modbus 슬레이브 ID
    /// </summary>
    public const byte DefaultModbusSlaveId = 1;

    /// <summary>
    /// 기본 Modbus 시작 주소
    /// </summary>
    public const ushort DefaultModbusStartAddress = 0;

    /// <summary>
    /// 기본 Modbus 수량
    /// </summary>
    public const ushort DefaultModbusQuantity = 1;

    #endregion

    #region Default Values

    /// <summary>
    /// 기본 패리티 설정
    /// </summary>
    public const string DefaultParity = "None";

    /// <summary>
    /// 기본 스톱 비트 설정
    /// </summary>
    public const string DefaultStopBits = "One";

    #endregion

    #region Status Messages

    /// <summary>
    /// 연결 안됨 상태 메시지
    /// </summary>
    public const string StatusDisconnected = "상태: 연결 안됨";

    /// <summary>
    /// 연결됨 상태 메시지
    /// </summary>
    public const string StatusConnected = "상태: 연결됨";

    /// <summary>
    /// 연결 오류 메시지 포맷
    /// </summary>
    public const string StatusConnectionErrorFormat = "연결 오류: {0}";

    /// <summary>
    /// 전송 오류 메시지 포맷
    /// </summary>
    public const string StatusSendErrorFormat = "전송 오류: {0}";

    /// <summary>
    /// 수신 오류 메시지 포맷
    /// </summary>
    public const string StatusReceiveErrorFormat = "수신 오류: {0}";

    /// <summary>
    /// Modbus 읽기 완료 메시지
    /// </summary>
    public const string StatusModbusReadComplete = "Modbus 읽기 완료";

    /// <summary>
    /// Modbus 입력 레지스터 읽기 완료 메시지
    /// </summary>
    public const string StatusModbusInputReadComplete = "Modbus 입력 레지스터 읽기 완료";

    /// <summary>
    /// Modbus 쓰기 완료 메시지
    /// </summary>
    public const string StatusModbusWriteComplete = "Modbus 쓰기 완료";

    /// <summary>
    /// Modbus 읽기 오류 메시지 포맷
    /// </summary>
    public const string StatusModbusReadErrorFormat = "Modbus 읽기 오류: {0}";

    /// <summary>
    /// Modbus 쓰기 오류 메시지 포맷
    /// </summary>
    public const string StatusModbusWriteErrorFormat = "Modbus 쓰기 오류: {0}";

    #endregion

    #region Error Messages

    /// <summary>
    /// Modbus 마스터 초기화되지 않음 오류 메시지
    /// </summary>
    public const string ErrorModbusMasterNotInitialized = "Modbus master not initialized";

    /// <summary>
    /// 시리얼 포트가 열리지 않음 오류 메시지
    /// </summary>
    public const string ErrorSerialPortNotOpen = "Serial port not open";

    /// <summary>
    /// 포인트 수 범위 오류 메시지
    /// </summary>
    public const string ErrorPointsOutOfRange = "Number of points must be between 1 and 125";

    /// <summary>
    /// Modbus 슬레이브 오류 메시지 포맷
    /// </summary>
    public const string ErrorModbusSlaveFormat = "Modbus slave error: SlaveId={0}, Address={1}, Count={2}, Error={3}";

    /// <summary>
    /// Modbus 통신 오류 메시지 포맷
    /// </summary>
    public const string ErrorModbusCommunicationFormat = "Modbus communication error: {0}";

    /// <summary>
    /// Modbus 타임아웃 오류 메시지 포맷 (읽기/쓰기)
    /// </summary>
    public const string ErrorModbusTimeoutReadWriteFormat = "Modbus timeout: SlaveId={0}, Address={1}, Count={2}";

    /// <summary>
    /// Modbus 타임아웃 오류 메시지 포맷 (단일 레지스터 쓰기)
    /// </summary>
    public const string ErrorModbusTimeoutSingleWriteFormat = "Modbus timeout: SlaveId={0}, Address={1}, Value={2}";

    /// <summary>
    /// Modbus 슬레이브 오류 메시지 포맷 (단일 레지스터 쓰기)
    /// </summary>
    public const string ErrorModbusSlaveSingleWriteFormat = "Modbus slave error: SlaveId={0}, Address={1}, Value={2}, Error={3}";

    #endregion

    #region Function Codes

    /// <summary>
    /// Modbus Function Code 03 - Read Holding Registers
    /// </summary>
    public const string FunctionCode03 = "03";

    /// <summary>
    /// Modbus Function Code 04 - Read Input Registers
    /// </summary>
    public const string FunctionCode04 = "04";

    /// <summary>
    /// Modbus Function Code 06 - Write Single Register
    /// </summary>
    public const string FunctionCode06 = "06";

    /// <summary>
    /// Modbus Function Code 16 - Write Multiple Registers
    /// </summary>
    public const string FunctionCode16 = "16";

    #endregion

    #region Data Types

    /// <summary>
    /// 기본 데이터 타입
    /// </summary>
    public const string DefaultDataType = "UINT16";

    #endregion

    #region Colors (Hex Values)

    /// <summary>
    /// 성공 색상 (녹색)
    /// </summary>
    public const string ColorSuccess = "#059669";

    /// <summary>
    /// 성공 배경 색상 (연한 녹색)
    /// </summary>
    public const string ColorSuccessBackground = "#DCFCE7";

    /// <summary>
    /// 오류 색상 (빨간색)
    /// </summary>
    public const string ColorError = "#DC2626";

    /// <summary>
    /// 오류 배경 색상 (연한 빨간색)
    /// </summary>
    public const string ColorErrorBackground = "#FEE2E2";

    #endregion

    #region UI Text Constants

    /// <summary>
    /// 시리얼 통신 타입 표시 텍스트
    /// </summary>
    public const string SerialCommunicationText = "일반 시리얼 통신";

    /// <summary>
    /// Modbus RTU 통신 타입 표시 텍스트
    /// </summary>
    public const string ModbusRtuCommunicationText = "모드버스 RTU 통신";

    /// <summary>
    /// 포트 선택 안됨 텍스트
    /// </summary>
    public const string PortNotSelectedText = "선택 안됨";

    /// <summary>
    /// 자동 읽기 시작 메시지
    /// </summary>
    public const string AutoReadStartMessage = "자동 읽기 시작";

    /// <summary>
    /// 자동 읽기 중지 메시지
    /// </summary>
    public const string AutoReadStopMessage = "자동 읽기 중지";

    /// <summary>
    /// 코일 읽기 미구현 메시지
    /// </summary>
    public const string CoilReadNotImplementedMessage = "코일 읽기 기능은 아직 구현되지 않았습니다.";

    /// <summary>
    /// 코일 쓰기 미구현 메시지
    /// </summary>
    public const string CoilWriteNotImplementedMessage = "코일 쓰기 기능은 아직 구현되지 않았습니다.";

    #endregion

    #region Settings

    /// <summary>
    /// 기본 Modbus 타임아웃 (밀리초)
    /// </summary>
    public const int DefaultModbusTimeout = 1000;

    /// <summary>
    /// 기본 언어 설정
    /// </summary>
    public const string DefaultLanguage = "한국어";

    /// <summary>
    /// 기본 로그 보존 기간
    /// </summary>
    public const string DefaultLogRetention = "7일";

    /// <summary>
    /// 기본 폴링 간격 표시 텍스트
    /// </summary>
    public const string DefaultPollingIntervalText = "1s";

    /// <summary>
    /// 기본 읽기 기능 코드 표시 텍스트
    /// </summary>
    public const string DefaultReadFunctionText = "03 - Read Holding Registers";

    /// <summary>
    /// 기본 쓰기 기능 코드 표시 텍스트
    /// </summary>
    public const string DefaultWriteFunctionText = "06 - Write Single Register";

    /// <summary>
    /// 기본 Modbus 프로토콜 표시 텍스트
    /// </summary>
    public const string DefaultModbusProtocolText = "Modbus RTU";

    #endregion

    #region String Constants

    /// <summary>
    /// 빈 문자열 교체용
    /// </summary>
    public const string EmptyString = "";

    #endregion
}
