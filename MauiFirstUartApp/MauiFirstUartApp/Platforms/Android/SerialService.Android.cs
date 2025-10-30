using MauiFirstUartApp.Core.Abstractions;
using UsbSerialForAndroid.Net;
using UsbSerialForAndroid.Net.Drivers;
using UsbSerialForAndroid.Net.Helper;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using NModbus;

namespace MauiFirstUartApp.Platforms.Android
{
    public class SerialService : ISerialService
    {
        private UsbDriverBase? _usbDriver;
        private IModbusSerialMaster? _modbusMaster;
        private CancellationTokenSource? _modbusPollingCts;
        public event Action<ushort[]>? ModbusPolled;


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

        public Task OpenAsync(string deviceName, int baudRate, int dataBits, int stopBits, int parity, SerialType serialType)
        {
            var items = UsbManagerHelper.GetAllUsbDevices();
            var device = items.FirstOrDefault(d => deviceName.StartsWith(d.DeviceName));
            if (device == null)
                throw new InvalidOperationException("Device not found");

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

            if (serialType == SerialType.Modbus)
            {
                _modbusPollingCts?.Cancel();
                _modbusPollingCts = new CancellationTokenSource();
                StartModbusPolling(_modbusPollingCts.Token);
            }
            else
            {
                _modbusPollingCts?.Cancel();
                _modbusPollingCts = null;
            }

            return Task.CompletedTask;
        }

        private void StartModbusPolling(CancellationToken token)
        {
            // 폴링 파라미터 예시
            byte slaveId = 1;
            ushort startAddress = 0;
            ushort numberOfPoints = 10;
            int pollingIntervalMs = 1000;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (_usbDriver != null)
                        {
                            // 실제 Modbus RTU 프레임 송수신 구현 필요
                            // 아래는 예시: ModbusReadHoldingRegistersAsync 호출
                            var result = await ModbusReadHoldingRegistersAsync(slaveId, startAddress, numberOfPoints);
                            ModbusPolled?.Invoke(result);
                        }
                    }
                    catch
                    {
                        // 필요시 에러 처리
                    }
                    await Task.Delay(pollingIntervalMs, token);
                }
            }, token);
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
            _modbusPollingCts?.Cancel();
            _modbusPollingCts = null;
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
            _modbusPollingCts?.Cancel();
            _modbusPollingCts = null;
            _usbDriver?.Close();
            _usbDriver = null;
            return ValueTask.CompletedTask;
        }

        public async Task<ushort[]> ModbusReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            if (_usbDriver == null)
                throw new InvalidOperationException("Serial port not open");
            if (_modbusMaster == null)
                throw new InvalidOperationException("Modbus master not initialized");

            // 입력 값 검증
            if (numberOfPoints == 0 || numberOfPoints > 125)
                throw new ArgumentException("Number of points must be between 1 and 125");

            try
            {
                return await Task.Run(() => _modbusMaster.ReadHoldingRegisters(slaveId, startAddress, numberOfPoints));
            }
            catch { throw new InvalidOperationException($"Modbus communication error"); }
            
        }


        public async Task<ushort[]> ModbusReadInputRegistersAsync(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            if (_usbDriver == null)
                throw new InvalidOperationException("Serial port not open");

            if (_modbusMaster == null)
                throw new InvalidOperationException("Modbus master not initialized");

            if (numberOfPoints == 0 || numberOfPoints > 125)
                throw new ArgumentException("Number of points must be between 1 and 125");

            try
            {
                return await Task.Run(() => _modbusMaster.ReadInputRegisters(slaveId, startAddress, numberOfPoints));
            }
            catch
            {
                throw new InvalidOperationException($"Modbus communication error");
            }

        }

        public async Task ModbusWriteSingleRegisterAsync(byte slaveId, ushort address, ushort value)
        {
            if (_usbDriver == null)
                throw new InvalidOperationException("Serial port not open");
            if (_modbusMaster == null)
                throw new InvalidOperationException("Modbus master not initialized");

            try
            {
                await Task.Run(() => _modbusMaster.WriteSingleRegister(slaveId, address, value));
            }
            catch
            {
                throw new InvalidOperationException($"Modbus communication error");
            }
        }


    }
}
