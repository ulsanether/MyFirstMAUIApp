using MauiFirstUartApp.Core.Abstractions;
using MauiFirstUartApp.Core.Constants;

using NModbus;
using NModbus.Serial;

using System.IO.Ports;

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
                ReadTimeout = SerialConstants.SerialReadTimeout,
                WriteTimeout = SerialConstants.SerialWriteTimeout,
                Handshake = Handshake.None,
                RtsEnable = true,
                DtrEnable = true
            };
            _port.Open();

            if (serialType == SerialType.Modbus)
            {
                var factory = new ModbusFactory();
                var adapter = new SerialPortAdapter(_port);
                _modbusMaster = factory.CreateRtuMaster(adapter);

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

        public Task StartModbusPollingAsync(byte slaveId, ushort startAddress, ushort numberOfPoints, int intervalMs = SerialConstants.DefaultModbusPollingInterval)
        {
            ValidateModbusMaster();

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
                        System.Diagnostics.Debug.WriteLine($"Modbus polling timeout: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
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
            ValidateSerialPort();
            _port!.BaseStream.Write(buffer, 0, buffer.Length);
            return Task.CompletedTask;
        }

        public async Task<byte[]> ReadAsync(CancellationToken ct = default)
        {
            ValidateSerialPort();
            var buf = new byte[SerialConstants.ReadBufferSize];
            int n = 0;
            try
            {
                n = await _port!.BaseStream.ReadAsync(buf, 0, buf.Length, ct);
            }
            catch { }
            if (n > 0)
            {
                var result = new byte[n];
                Array.Copy(buf, result, n);
                return result;
            }
            return [];
        }

        public async Task<ushort[]> ModbusReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            ValidateModbusMaster();
            ValidateModbusPointsRange(numberOfPoints);

            return await ExecuteModbusOperation(
                () => _modbusMaster!.ReadHoldingRegisters(slaveId, startAddress, numberOfPoints),
                slaveId,
                startAddress,
                numberOfPoints
            );
        }

        public async Task<ushort[]> ModbusReadInputRegistersAsync(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            ValidateModbusMaster();
            ValidateModbusPointsRange(numberOfPoints);

            return await ExecuteModbusOperation(
                () => _modbusMaster!.ReadInputRegisters(slaveId, startAddress, numberOfPoints),
                slaveId,
                startAddress,
                numberOfPoints
            );
        }

        public async Task ModbusWriteSingleRegisterAsync(byte slaveId, ushort address, ushort value)
        {
            ValidateModbusMaster();

            await ExecuteModbusWriteOperation(
                () => _modbusMaster!.WriteSingleRegister(slaveId, address, value),
                slaveId,
                address,
                value
            );
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

        #region Private Helper Methods

        /// <summary>
        /// Modbus 마스터 초기화 상태 검증
        /// </summary>
        private void ValidateModbusMaster()
        {
            if (_modbusMaster == null)
                throw new InvalidOperationException(SerialConstants.ErrorModbusMasterNotInitialized);
        }

        /// <summary>
        /// 시리얼 포트 연결 상태 검증
        /// </summary>
        private void ValidateSerialPort()
        {
            if (_port == null || !_port.IsOpen)
                throw new InvalidOperationException(SerialConstants.ErrorSerialPortNotOpen);
        }

        /// <summary>
        /// Modbus 포인트 수 범위 검증
        /// </summary>
        private static void ValidateModbusPointsRange(ushort numberOfPoints)
        {
            if (numberOfPoints < SerialConstants.ModbusMinPoints || numberOfPoints > SerialConstants.ModbusMaxPoints)
                throw new ArgumentException(SerialConstants.ErrorPointsOutOfRange);
        }

        /// <summary>
        /// Modbus 읽기 작업 공통 예외 처리
        /// </summary>
        private async Task<ushort[]> ExecuteModbusOperation(
            Func<ushort[]> operation,
            byte slaveId,
            ushort startAddress,
            ushort numberOfPoints)
        {
            try
            {
                return await Task.Run(operation);
            }
            catch (SlaveException ex)
            {
                throw new InvalidOperationException(
                    string.Format(SerialConstants.ErrorModbusSlaveFormat, slaveId, startAddress, numberOfPoints, ex.Message),
                    ex);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException(
                    string.Format(SerialConstants.ErrorModbusCommunicationFormat, ex.Message),
                    ex);
            }
            catch (TimeoutException ex)
            {
                throw new InvalidOperationException(
                    string.Format(SerialConstants.ErrorModbusTimeoutReadWriteFormat, slaveId, startAddress, numberOfPoints),
                    ex);
            }
        }

        /// <summary>
        /// Modbus 쓰기 작업 공통 예외 처리
        /// </summary>
        private async Task ExecuteModbusWriteOperation(
            Action operation,
            byte slaveId,
            ushort address,
            ushort value)
        {
            try
            {
                await Task.Run(operation);
            }
            catch (SlaveException ex)
            {
                throw new InvalidOperationException(
                    string.Format(SerialConstants.ErrorModbusSlaveSingleWriteFormat, slaveId, address, value, ex.Message),
                    ex);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException(
                    string.Format(SerialConstants.ErrorModbusCommunicationFormat, ex.Message),
                    ex);
            }
            catch (TimeoutException ex)
            {
                throw new InvalidOperationException(
                    string.Format(SerialConstants.ErrorModbusTimeoutSingleWriteFormat, slaveId, address, value),
                    ex);
            }
        }

        #endregion
    }
}
