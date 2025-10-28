using MauiFirstUartApp.Core.Abstractions;

using System.IO.Ports;

namespace MauiFirstUartApp.Platforms.Windows
{
    public class SerialService : ISerialService
    {
        private SerialPort? _port;

        public Task<IReadOnlyList<string>> GetDeviceNamesAsync()
        {
            var names = SerialPort.GetPortNames().ToList();
            return Task.FromResult((IReadOnlyList<string>)names);
        }

        public Task OpenAsync(string portName, int baudRate, int dataBits, int stopBits, int parity)
        {
            if (_port != null && _port.IsOpen)
                _port.Close();

            // Enum 변환
            var parityEnum = (SerialParity)parity;
            var stopBitsEnum = (SerialStopBits)stopBits;

            // .NET Parity 변환
            Parity netParity = parityEnum switch
            {
                SerialParity.None => Parity.None,
                SerialParity.Odd => Parity.Odd,
                SerialParity.Even => Parity.Even,
                SerialParity.Mark => Parity.Mark,
                SerialParity.Space => Parity.Space,
                _ => Parity.None
            };

            // .NET StopBits 변환
            StopBits netStopBits = stopBitsEnum switch
            {
                SerialStopBits.One => StopBits.One,
                SerialStopBits.OnePointFive => StopBits.OnePointFive,
                SerialStopBits.Two => StopBits.Two,
                _ => StopBits.One
            };

            _port = new SerialPort(portName, baudRate, netParity, dataBits, netStopBits)
            {
                ReadTimeout = 200,
                WriteTimeout = 200
            };
            _port.Open();
            return Task.CompletedTask;
        }

        public Task WriteAsync(byte[] buffer, CancellationToken ct = default)
        {
            if (_port == null || !_port.IsOpen)
                throw new InvalidOperationException("Serial port not open");
            _port.BaseStream.Write(buffer, 0, buffer.Length);
            return Task.CompletedTask;
        }

        public async Task<byte[]> ReadAsync(CancellationToken ct = default)
        {
            if (_port == null || !_port.IsOpen)
                throw new InvalidOperationException("Serial port not open");
            var buf = new byte[4096];
            int n = 0;
            try
            {
                n = await _port.BaseStream.ReadAsync(buf, 0, buf.Length, ct);
            }
            catch { }
            if (n > 0)
            {
                var result = new byte[n];
                System.Array.Copy(buf, result, n);
                return result;
            }
            return [];
        }

        public Task CloseAsync()
        {
            _port?.Close();
            _port = null;
            return Task.CompletedTask;
        }

        public Task<bool> IsConnectedAsync()
        {
            return Task.FromResult(_port != null && _port.IsOpen);
        }

        public ValueTask DisposeAsync()
        {
            _port?.Dispose();
            _port = null;
            return ValueTask.CompletedTask;
        }
    }
}
