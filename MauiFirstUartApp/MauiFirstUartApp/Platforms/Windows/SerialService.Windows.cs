using MauiFirstUartApp.Core.Abstractions;

using NModbus;
using NModbus.Serial;

using System.IO.Ports;
using System.Threading;

using Windows.ApplicationModel.Email;
using Windows.UI.Popups;

namespace MauiFirstUartApp.Platforms.Windows
{
    public class SerialService : ISerialService
    {
        private SerialPort? _port;
        private IModbusSerialMaster? _modbusMaster;
        private CancellationTokenSource? _modbusPollingCts;

        public event Action<ushort[]>? ModbusPolled;

        public Task<IReadOnlyList<string>> GetDeviceNamesAsync()
        {
            var names = SerialPort.GetPortNames().ToList();
            return Task.FromResult((IReadOnlyList<string>)names);
        }

        public Task OpenAsync(string portName, int baudRate, int dataBits, int stopBits, int parity, SerialType serialType)
        {
            if (_port != null && _port.IsOpen)
                _port.Close();

            var parityEnum = (SerialParity)parity;
            var stopBitsEnum = (SerialStopBits)stopBits;

            Parity netParity = parityEnum switch
            {
                SerialParity.None => Parity.None,
                SerialParity.Odd => Parity.Odd,
                SerialParity.Even => Parity.Even,
                SerialParity.Mark => Parity.Mark,
                SerialParity.Space => Parity.Space,
                _ => Parity.None
            };

            StopBits netStopBits = stopBitsEnum switch
            {
                SerialStopBits.One => StopBits.One,
                SerialStopBits.OnePointFive => StopBits.OnePointFive,
                SerialStopBits.Two => StopBits.Two,
                _ => StopBits.One
            };

            _port = new SerialPort(portName, baudRate, netParity, dataBits, netStopBits)
            {
                ReadTimeout = 3000,
                WriteTimeout = 3000,
                Handshake = Handshake.None,
                RtsEnable = true,
                DtrEnable = true
            };
            _port.Open();

            if (serialType == SerialType.Modbus)
            {
                var factory = new ModbusFactory();
                var adapter = new SerialPortAdapter(_port); // 수정된 부분
                _modbusMaster = factory.CreateRtuMaster(adapter);
       
                // 기존 폴링이 있다면 중지
                _modbusPollingCts?.Cancel();
                _modbusPollingCts = null;
            }
            else
            {
                _modbusMaster = null;
                _modbusPollingCts?.Cancel();
                _modbusPollingCts = null;
            }

            return Task.CompletedTask;
        }

        // 누락된 StartModbusPollingAsync 메서드 추가
        public Task StartModbusPollingAsync(byte slaveId, ushort startAddress, ushort numberOfPoints, int intervalMs = 1000)
        {
            if (_modbusMaster == null)
                throw new InvalidOperationException("Modbus master not initialized");

            // 기존 폴링 중지
            _modbusPollingCts?.Cancel();
            _modbusPollingCts = new CancellationTokenSource();

            StartModbusPolling(slaveId, startAddress, numberOfPoints, intervalMs, _modbusPollingCts.Token);
            return Task.CompletedTask;
        }

        public Task StopModbusPollingAsync()
        {
            _modbusPollingCts?.Cancel();
            _modbusPollingCts = null;
            return Task.CompletedTask;
        }

        public Task<bool> IsModbusPollingActiveAsync()
        {
            return Task.FromResult(_modbusPollingCts != null && !_modbusPollingCts.Token.IsCancellationRequested);
        }

        // 중복 제거: 하나의 StartModbusPolling 메서드만 유지
        private void StartModbusPolling(byte slaveId, ushort startAddress, ushort numberOfPoints, int pollingIntervalMs, CancellationToken token)
        {
            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (_modbusMaster != null)
                        {
                            var result = await ModbusReadHoldingRegistersAsync(slaveId, startAddress, numberOfPoints);
                            ModbusPolled?.Invoke(result);
                        }
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Modbus timeout"))
                    {
                        // 타임아웃은 일시적 오류로 간주하고 계속 진행
                        System.Diagnostics.Debug.WriteLine($"Modbus polling timeout: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 기타 오류 로깅
                        System.Diagnostics.Debug.WriteLine($"Modbus polling error: {ex.Message}");
                    }

                    try
                    {
                        await Task.Delay(pollingIntervalMs, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, token);
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

        public async Task<ushort[]> ModbusReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            if (_modbusMaster == null)
                throw new InvalidOperationException("Modbus master not initialized");


            if (numberOfPoints == 0 || numberOfPoints > 125)
                throw new ArgumentException("Number of points must be between 1 and 125");

            try
            {
                var values = await Task.Run(() => _modbusMaster.ReadHoldingRegisters(slaveId, startAddress, numberOfPoints));
                return values;

            }
            catch (NModbus.SlaveException ex)
            {
                throw new InvalidOperationException($"Modbus slave error: SlaveId={slaveId}, Address={startAddress}, Count={numberOfPoints}, Error={ex.Message}", ex);
            }
            catch (System.IO.IOException ex)
            {
                throw new InvalidOperationException($"Modbus communication error: {ex.Message}", ex);
            }
            catch (TimeoutException ex)
            {
                throw new InvalidOperationException($"Modbus timeout: SlaveId={slaveId}, Address={startAddress}, Count={numberOfPoints}", ex);
            }
        }

        public async Task<ushort[]> ModbusReadInputRegistersAsync(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            if (_modbusMaster == null)
                throw new InvalidOperationException("Modbus master not initialized");
            
            if (numberOfPoints == 0 || numberOfPoints > 125)
                throw new ArgumentException("Number of points must be between 1 and 125");
            try
            {
                return await Task.Run(() => _modbusMaster.ReadInputRegisters(slaveId, startAddress, numberOfPoints));
            }
            catch (NModbus.SlaveException ex)
            {
                throw new InvalidOperationException($"Modbus slave error: SlaveId={slaveId}, Address={startAddress}, Count={numberOfPoints}, Error={ex.Message}", ex);
            }
            catch (System.IO.IOException ex)
            {
                throw new InvalidOperationException($"Modbus communication error: {ex.Message}", ex);
            }
            catch (TimeoutException ex)
            {
                throw new InvalidOperationException($"Modbus timeout: SlaveId={slaveId}, Address={startAddress}, Count={numberOfPoints}", ex);
            }
        }

        public async Task ModbusWriteSingleRegisterAsync(byte slaveId, ushort address, ushort value)
        {
            if (_modbusMaster == null)
                throw new InvalidOperationException("Modbus master not initialized");

            try
            {
                await Task.Run(() => _modbusMaster.WriteSingleRegister(slaveId, address, value));
            }
            catch (NModbus.SlaveException ex)
            {
                throw new InvalidOperationException($"Modbus slave error: SlaveId={slaveId}, Address={address}, Value={value}, Error={ex.Message}", ex);
            }
            catch (System.IO.IOException ex)
            {
                throw new InvalidOperationException($"Modbus communication error: {ex.Message}", ex);
            }
            catch (TimeoutException ex)
            {
                throw new InvalidOperationException($"Modbus timeout: SlaveId={slaveId}, Address={address}, Value={value}", ex);
            }
        }

        public Task CloseAsync()
        {
            _modbusPollingCts?.Cancel();
            _modbusPollingCts = null;
            _port?.Close();
            _port = null;
            _modbusMaster = null;
            return Task.CompletedTask;
        }

        public Task<bool> IsConnectedAsync()
        {
            return Task.FromResult(_port != null && _port.IsOpen);
        }

        public ValueTask DisposeAsync()
        {
            _modbusPollingCts?.Cancel();
            _modbusPollingCts = null;   
            _port?.Dispose();
            _port = null;
            _modbusMaster = null;
            return ValueTask.CompletedTask;
        }
    }
}
