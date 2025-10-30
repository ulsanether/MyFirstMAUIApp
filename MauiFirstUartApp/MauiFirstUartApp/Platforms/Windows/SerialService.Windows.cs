using MauiFirstUartApp.Core.Abstractions;

using NModbus;
using NModbus.Serial;

using System.IO.Ports;
using System.Threading;

namespace MauiFirstUartApp.Platforms.Windows
{
    public class SerialService : ISerialService
    {
        private SerialPort? _port;
        private IModbusSerialMaster? _modbusMaster;
        private CancellationTokenSource? _modbusPollingCts;

        // 폴링 결과를 알리기 위한 이벤트(옵션)
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
                ReadTimeout = 1000,  // 타임아웃 증가
                WriteTimeout = 1000
            };
            _port.Open();

            if (serialType == SerialType.Modbus)
            {
                var factory = new ModbusFactory();
                _modbusMaster = factory.CreateRtuMaster(_port);
                _modbusMaster.Transport.Retries = 3;  // 재시도 횟수 증가
                _modbusMaster.Transport.ReadTimeout = 1000;  // 읽기 타임아웃 설정
                _modbusMaster.Transport.WriteTimeout = 1000; // 쓰기 타임아웃 설정

                // 폴링은 실제 디바이스가 연결되었을 때만 시작하도록 주석 처리
                // _modbusPollingCts?.Cancel();
                // _modbusPollingCts = new CancellationTokenSource();
                // StartModbusPolling(_modbusPollingCts.Token);
            }
            else
            {
                _modbusMaster = null;
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
                        if (_modbusMaster != null)
                        {
                            var result = _modbusMaster.ReadHoldingRegisters(slaveId, startAddress, numberOfPoints);
                            ModbusPolled?.Invoke(result); // 이벤트로 결과 전달(옵션)
                        }
                    }
                    catch (Exception ex)
                    {
                        // Modbus 통신 오류 로깅
                        System.Diagnostics.Debug.WriteLine($"Modbus polling error: {ex.Message}");
                    }
                    await Task.Delay(pollingIntervalMs, token);
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

            // 입력 값 검증
            if (numberOfPoints == 0 || numberOfPoints > 125)
                throw new ArgumentException("Number of points must be between 1 and 125");

            try
            {
                return await Task.Run(() => _modbusMaster.ReadHoldingRegisters(slaveId, startAddress, numberOfPoints));
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
            // 입력 값 검증
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

