using MauiFirstUartApp.Core.Abstractions;
using UsbSerialForAndroid.Net;
using UsbSerialForAndroid.Net.Drivers;
using UsbSerialForAndroid.Net.Helper;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace MauiFirstUartApp.Platforms.Android
{
    public class SerialService : ISerialService
    {
        private UsbDriverBase? _usbDriver;

        public SerialService()
        {
            UsbDriverFactory.RegisterUsbBroadcastReceiver();
        }

        public Task<IReadOnlyList<string>> GetDeviceNamesAsync()
        {
            var items = UsbManagerHelper.GetAllUsbDevices();
            foreach (var item in items)
            {
                if (!UsbManagerHelper.HasPermission(item))
                    UsbManagerHelper.RequestPermission(item);
            }
            var names = items.Select(d => $"{d.DeviceName} ({d.ProductName ?? "Unknown"})").ToList();
            return Task.FromResult((IReadOnlyList<string>)names);
        }

        public Task OpenAsync(string deviceName, int baudRate, int dataBits, int stopBits, int parity)
        {
            var items = UsbManagerHelper.GetAllUsbDevices();
            var device = items.FirstOrDefault(d => deviceName.StartsWith(d.DeviceName));
            if (device == null)
                throw new InvalidOperationException("Device not found");

            // Enum 변환
            var stopBitsEnum = (SerialStopBits)stopBits;
            var parityEnum = (SerialParity)parity;

            var _stopBits = stopBitsEnum switch
            {
                SerialStopBits.One => UsbSerialForAndroid.Net.Enums.StopBits.One,
                SerialStopBits.OnePointFive => UsbSerialForAndroid.Net.Enums.StopBits.OnePointFive,
                SerialStopBits.Two => UsbSerialForAndroid.Net.Enums.StopBits.Two,
                _ => UsbSerialForAndroid.Net.Enums.StopBits.One
            };
            var _parity = parityEnum switch
            {
                SerialParity.None => UsbSerialForAndroid.Net.Enums.Parity.None,
                SerialParity.Odd => UsbSerialForAndroid.Net.Enums.Parity.Odd,
                SerialParity.Even => UsbSerialForAndroid.Net.Enums.Parity.Even,
                SerialParity.Mark => UsbSerialForAndroid.Net.Enums.Parity.Mark,
                SerialParity.Space => UsbSerialForAndroid.Net.Enums.Parity.Space,
                _ => UsbSerialForAndroid.Net.Enums.Parity.None
            };

            _usbDriver = UsbDriverFactory.CreateUsbDriver(device.DeviceId);
            _usbDriver.Open(baudRate, (byte)dataBits, _stopBits, _parity);
            return Task.CompletedTask;
        }

        public Task WriteAsync(byte[] buffer, CancellationToken ct = default)
        {
            if (_usbDriver == null)
                throw new InvalidOperationException("Serial port not open");
            _usbDriver.Write(buffer);
            return Task.CompletedTask;
        }

        public Task<byte[]> ReadAsync(CancellationToken ct = default)
        {
            if (_usbDriver == null)
                throw new InvalidOperationException("Serial port not open");
            var buffer = _usbDriver.Read();
            return Task.FromResult(buffer ?? Array.Empty<byte>());
        }

        public Task CloseAsync()
        {
            _usbDriver?.Close();
            _usbDriver = null;
            return Task.CompletedTask;
        }

        public Task<bool> IsConnectedAsync()
        {
            if (_usbDriver == null) return Task.FromResult(false);
            try { return Task.FromResult(_usbDriver.TestConnection()); }
            catch { return Task.FromResult(false); }
        }

        public ValueTask DisposeAsync()
        {
            _usbDriver?.Close();
            _usbDriver = null;
            return ValueTask.CompletedTask;
        }
    }
}
