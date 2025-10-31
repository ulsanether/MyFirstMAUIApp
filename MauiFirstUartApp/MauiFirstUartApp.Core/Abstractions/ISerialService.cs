namespace MauiFirstUartApp.Core.Abstractions;

public interface ISerialService : IAsyncDisposable
{
    // 기존 이벤트
    event Action<ushort[]>? ModbusPolled;

    Task<IReadOnlyList<string>> GetDeviceNamesAsync();
    public Task OpenAsync(string portName, int baudRate, int dataBits, int stopBits, int parity, SerialType serialType);
    Task WriteAsync(byte[] buffer, CancellationToken ct = default);
    Task<byte[]> ReadAsync(CancellationToken ct = default);
    Task CloseAsync();
    Task<bool> IsConnectedAsync();

    // 모드버스 기능 추가
    Task<ushort[]> ModbusReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort numberOfPoints);
    Task<ushort[]> ModbusReadInputRegistersAsync(byte slaveId, ushort startAddress, ushort numberOfPoints);
    Task ModbusWriteSingleRegisterAsync(byte slaveId, ushort address, ushort value);

    // 폴링 제어 기능 추가
    Task StartModbusPollingAsync(byte slaveId, ushort startAddress, ushort numberOfPoints, int intervalMs = 1000);
    Task StopModbusPollingAsync();
    Task<bool> IsModbusPollingActiveAsync();
}
