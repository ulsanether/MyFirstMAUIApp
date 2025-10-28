namespace MauiFirstUartApp.Core.Abstractions;

public interface ISerialService : IAsyncDisposable
{
    Task<IReadOnlyList<string>> GetDeviceNamesAsync();
    Task OpenAsync(string deviceName, int baudRate, int dataBits, int stopBits, int parity);
    Task WriteAsync(byte[] buffer, CancellationToken ct = default);
    Task<byte[]> ReadAsync(CancellationToken ct = default);
    Task CloseAsync();
    Task<bool> IsConnectedAsync();
}
