using MauiFirstUartApp.Core.Abstractions;

using System.Collections.ObjectModel;
using System.Text;

namespace MauiFirstUartApp.ViewModels;

public class MainPageViewModel : BindableObject
{
    private readonly ISerialService _serialService;

    public ObservableCollection<string> PortNames { get; } = new();
    public ObservableCollection<string> ParityOptions { get; } = new() { "None", "Odd", "Even", "Mark", "Space" };
    public ObservableCollection<string> StopBitsOptions { get; } = new() { "One", "OnePointFive", "Two" };

    public ObservableCollection<string> ReceivedData { get; } = new();

    public MainPageViewModel(ISerialService serialService)
    {
        _serialService = serialService;
    }

    public async Task InitializeAsync()
    {
        PortNames.Clear();
        var ports = await _serialService.GetDeviceNamesAsync();
        foreach (var port in ports)
            PortNames.Add(port);
    }

    public async Task<bool> ConnectAsync(string? portName, int baud, int dataBits, string? parity, string? stopBits)
    {
        if (string.IsNullOrEmpty(portName)) return false;
        int parityVal = ParityOptions.IndexOf(parity ?? "None");
        int stopBitsVal = StopBitsOptions.IndexOf(stopBits ?? "One");
        try
        {
            await _serialService.OpenAsync(portName, baud, dataBits, stopBitsVal, parityVal);
            _ = ReadLoopAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        await _serialService.CloseAsync();
    }

    public async Task SendAsync(string data)
    {
        if (string.IsNullOrEmpty(data)) return;
        var bytes = Encoding.UTF8.GetBytes(data);
        await _serialService.WriteAsync(bytes);
    }

    private async Task ReadLoopAsync()
    {
        var ct = new CancellationTokenSource();
        while (await _serialService.IsConnectedAsync())
        {
            var received = await _serialService.ReadAsync(ct.Token);
            if (received != null && received.Length > 0)
            {
                var str = Encoding.UTF8.GetString(received);
                ReceivedData.Add(str);
            }
            await Task.Delay(100);
        }
    }
}
