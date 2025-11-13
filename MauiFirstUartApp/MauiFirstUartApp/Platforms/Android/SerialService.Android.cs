using MauiFirstUartApp.Core.Abstractions;
using MauiFirstUartApp.Core.Constants;

using UsbSerialForAndroid.Net;
using UsbSerialForAndroid.Net.Drivers;
using UsbSerialForAndroid.Net.Helper;

namespace MauiFirstUartApp.Platforms.Android
{
    public class SerialService : ISerialService
    {
        private UsbDriverBase? _usbDriver;
        private CancellationTokenSource? _modbusPollingCts;
        private readonly SemaphoreSlim _txLock = new(1, 1);

        private readonly int _readTimeoutMs = 2000;
        private readonly int _postWriteDelayMs = 10;

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
                _modbusPollingCts = null;
            }
            else
            {
                _modbusPollingCts?.Cancel();
                _modbusPollingCts = null;
            }

            return Task.CompletedTask;
        }

        public Task StartModbusPollingAsync(byte slaveId, ushort startAddress, ushort numberOfPoints, int intervalMs = SerialConstants.DefaultModbusPollingInterval)
        {
            ValidateUsbDriver();

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
                        if (_usbDriver != null)
                        {
                            var result = await ModbusReadHoldingRegistersAsync(slaveId, startAddress, numberOfPoints);
                            ModbusPolled?.Invoke(result);
                        }
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
            ValidateUsbDriver();
            _usbDriver!.Write(buffer);
            return Task.CompletedTask;
        }

        public Task<byte[]> ReadAsync(CancellationToken ct = default)
        {
            ValidateUsbDriver();
            var buffer = _usbDriver!.Read();
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
            ValidateUsbDriver();
            ValidateModbusPointsRange(numberOfPoints);

            return await ExecuteModbusReadOperation(
                async () => await ModbusReadHoldingRegistersInternalAsync(slaveId, startAddress, numberOfPoints),
                slaveId,
                startAddress,
                numberOfPoints
            );
        }

        public async Task<ushort[]> ModbusReadInputRegistersAsync(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            ValidateUsbDriver();
            ValidateModbusPointsRange(numberOfPoints);

            return await ExecuteModbusReadOperation(
                async () => await ModbusReadInputRegistersInternalAsync(slaveId, startAddress, numberOfPoints),
                slaveId,
                startAddress,
                numberOfPoints
            );
        }

        public async Task ModbusWriteSingleRegisterAsync(byte slaveId, ushort address, ushort value)
        {
            ValidateUsbDriver();

            await ExecuteModbusWriteOperation(
                async () => await ModbusWriteSingleRegisterInternalAsync(slaveId, address, value),
                slaveId,
                address,
                value
            );
        }

        #region Private Helper Methods

        /// <summary>
        /// USB 드라이버 초기화 상태 검증
        /// </summary>
        private void ValidateUsbDriver()
        {
            if (_usbDriver == null)
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
        private async Task<ushort[]> ExecuteModbusReadOperation(
            Func<Task<ushort[]>> operation,
            byte slaveId,
            ushort startAddress,
            ushort numberOfPoints)
        {
            try
            {
                return await operation();
            }
            catch (TimeoutException ex)
            {
                throw new InvalidOperationException(
                    string.Format(SerialConstants.ErrorModbusTimeoutReadWriteFormat, slaveId, startAddress, numberOfPoints),
                    ex);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("CRC mismatch") || ex.Message.Contains("Modbus exception"))
            {
                throw new InvalidOperationException(
                    string.Format(SerialConstants.ErrorModbusSlaveFormat, slaveId, startAddress, numberOfPoints, ex.Message),
                    ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    string.Format(SerialConstants.ErrorModbusCommunicationFormat, ex.Message),
                    ex);
            }
        }

        /// <summary>
        /// Modbus 쓰기 작업 공통 예외 처리
        /// </summary>
        private async Task ExecuteModbusWriteOperation(
            Func<Task> operation,
            byte slaveId,
            ushort address,
            ushort value)
        {
            try
            {
                await operation();
            }
            catch (TimeoutException ex)
            {
                throw new InvalidOperationException(
                    string.Format(SerialConstants.ErrorModbusTimeoutSingleWriteFormat, slaveId, address, value),
                    ex);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("CRC mismatch") || ex.Message.Contains("Modbus exception"))
            {
                throw new InvalidOperationException(
                    string.Format(SerialConstants.ErrorModbusSlaveSingleWriteFormat, slaveId, address, value, ex.Message),
                    ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    string.Format(SerialConstants.ErrorModbusCommunicationFormat, ex.Message),
                    ex);
            }
        }

        #endregion

        #region Modbus RTU Implementation

        /// <summary>
        /// CRC16 계산 (Modbus용)
        /// </summary>
        private static ushort Crc16(byte[] data, int length)
        {
            ushort crc = 0xFFFF;
            for (int pos = 0; pos < length; pos++)
            {
                crc ^= data[pos];
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            return crc;
        }

        /// <summary>
        /// 헤더와 바디를 순차적으로 읽는 응답 처리 유틸리티
        /// </summary>
        private async Task<byte[]> ReadResponseHeaderAndBodyAsync(int headerLen, Func<byte[], int> getRemainingLengthFromHeader, int timeoutMs)
        {
            ValidateUsbDriver();

            var buffer = new List<byte>();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // 헤더 바이트 수집
            while (buffer.Count < headerLen && sw.ElapsedMilliseconds < timeoutMs)
            {
                var chunk = _usbDriver!.Read();
                if (chunk != null && chunk.Length > 0)
                {
                    buffer.AddRange(chunk);
                }
                else
                {
                    await Task.Delay(10).ConfigureAwait(false);
                }
            }

            if (buffer.Count < headerLen)
                throw new TimeoutException("Timeout while waiting for response header");

            // 전체 바디 길이 계산
            var headerArr = buffer.Take(headerLen).ToArray();
            int remaining = getRemainingLengthFromHeader(headerArr);
            if (remaining < 0) throw new InvalidOperationException("Invalid header or cannot determine remaining length");

            // 남은 바이트 수집
            while (buffer.Count < headerLen + remaining && sw.ElapsedMilliseconds < timeoutMs)
            {
                var chunk = _usbDriver!.Read();
                if (chunk != null && chunk.Length > 0)
                {
                    buffer.AddRange(chunk);
                }
                else
                {
                    await Task.Delay(10).ConfigureAwait(false);
                }
            }

            if (buffer.Count < headerLen + remaining)
                throw new TimeoutException("Timeout while waiting for full response");

            return buffer.ToArray();
        }

        /// <summary>
        /// Modbus Read Holding Registers 내부 구현 (Function 0x03)
        /// </summary>
        private async Task<ushort[]> ModbusReadHoldingRegistersInternalAsync(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            await _txLock.WaitAsync().ConfigureAwait(false);
            try
            {
                // 요청 빌드
                byte[] req = new byte[8];
                req[0] = slaveId;
                req[1] = 0x03;
                req[2] = (byte)(startAddress >> 8);
                req[3] = (byte)(startAddress & 0xFF);
                req[4] = (byte)(numberOfPoints >> 8);
                req[5] = (byte)(numberOfPoints & 0xFF);
                ushort crc = Crc16(req, 6);
                req[6] = (byte)(crc & 0xFF);
                req[7] = (byte)(crc >> 8);

                // 수신 버퍼 클리어
                try { _usbDriver!.Read(); } catch { }

                // 요청 전송
                _usbDriver!.Write(req);
                await Task.Delay(_postWriteDelayMs).ConfigureAwait(false);

                // 응답 읽기
                byte[] full = await ReadResponseHeaderAndBodyAsync(3, header =>
                {
                    int byteCount = header[2];
                    return byteCount + 2; // 데이터 + CRC
                }, _readTimeoutMs).ConfigureAwait(false);

                return ProcessReadResponse(full, slaveId, numberOfPoints);
            }
            finally
            {
                _txLock.Release();
            }
        }

        /// <summary>
        /// Modbus Read Input Registers 내부 구현 (Function 0x04)
        /// </summary>
        private async Task<ushort[]> ModbusReadInputRegistersInternalAsync(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            await _txLock.WaitAsync().ConfigureAwait(false);
            try
            {
                byte[] req = new byte[8];
                req[0] = slaveId;
                req[1] = 0x04;
                req[2] = (byte)(startAddress >> 8);
                req[3] = (byte)(startAddress & 0xFF);
                req[4] = (byte)(numberOfPoints >> 8);
                req[5] = (byte)(numberOfPoints & 0xFF);
                ushort crc = Crc16(req, 6);
                req[6] = (byte)(crc & 0xFF);
                req[7] = (byte)(crc >> 8);

                try { _usbDriver!.Read(); } catch { }

                _usbDriver!.Write(req);
                await Task.Delay(_postWriteDelayMs).ConfigureAwait(false);

                byte[] full = await ReadResponseHeaderAndBodyAsync(3, header =>
                {
                    int byteCount = header[2];
                    return byteCount + 2;
                }, _readTimeoutMs).ConfigureAwait(false);

                return ProcessReadResponse(full, slaveId, numberOfPoints);
            }
            finally
            {
                _txLock.Release();
            }
        }

        /// <summary>
        /// Modbus Write Single Register 내부 구현 (Function 0x06)
        /// </summary>
        private async Task ModbusWriteSingleRegisterInternalAsync(byte slaveId, ushort address, ushort value)
        {
            await _txLock.WaitAsync().ConfigureAwait(false);
            try
            {
                byte[] req = new byte[8];
                req[0] = slaveId;
                req[1] = 0x06;
                req[2] = (byte)(address >> 8);
                req[3] = (byte)(address & 0xFF);
                req[4] = (byte)(value >> 8);
                req[5] = (byte)(value & 0xFF);
                ushort crc = Crc16(req, 6);
                req[6] = (byte)(crc & 0xFF);
                req[7] = (byte)(crc >> 8);

                try { _usbDriver!.Read(); } catch { }

                _usbDriver!.Write(req);
                await Task.Delay(_postWriteDelayMs).ConfigureAwait(false);

                // 응답은 요청을 에코함 (8바이트)
                byte[] full = await ReadResponseHeaderAndBodyAsync(8, header => 0, _readTimeoutMs).ConfigureAwait(false);

                ProcessWriteResponse(full, slaveId);
            }
            finally
            {
                _txLock.Release();
            }
        }

        /// <summary>
        /// 읽기 응답 처리 공통 로직
        /// </summary>
        private static ushort[] ProcessReadResponse(byte[] full, byte slaveId, ushort numberOfPoints)
        {
            if (full.Length < 5) throw new InvalidOperationException("Invalid response length");
            if (full[0] != slaveId) throw new InvalidOperationException($"Unexpected slave id in response: {full[0]}");

            byte func = full[1];
            if ((func & 0x80) != 0)
            {
                byte ex = full.Length >= 4 ? full[3] : (byte)0;
                throw new InvalidOperationException($"Modbus exception from slave {slaveId}: function {func & 0x7F}, code {ex}");
            }

            byte byteCountResp = full[2];
            int payloadLen = 3 + byteCountResp;
            ushort calcCrc = Crc16(full, payloadLen);
            ushort recvCrc = (ushort)(full[payloadLen] | (full[payloadLen + 1] << 8));
            if (calcCrc != recvCrc) throw new InvalidOperationException($"CRC mismatch. Calc=0x{calcCrc:X4}, Recv=0x{recvCrc:X4}");

            int regCount = byteCountResp / 2;
            var regs = new ushort[regCount];
            for (int i = 0; i < regCount; i++)
            {
                int idx = 3 + i * 2;
                regs[i] = (ushort)((full[idx] << 8) | full[idx + 1]);
            }

            if (regs.Length != numberOfPoints)
            {
                var arr = new ushort[numberOfPoints];
                Array.Copy(regs, 0, arr, 0, Math.Min(regs.Length, numberOfPoints));
                return arr;
            }

            return regs;
        }

        /// <summary>
        /// 쓰기 응답 처리 공통 로직
        /// </summary>
        private static void ProcessWriteResponse(byte[] full, byte slaveId)
        {
            if (full.Length < 8) throw new InvalidOperationException("Invalid response length");

            ushort calcCrc = Crc16(full, 6);
            ushort recvCrc = (ushort)(full[6] | (full[7] << 8));
            if (calcCrc != recvCrc) throw new InvalidOperationException($"CRC mismatch. Calc=0x{calcCrc:X4}, Recv=0x{recvCrc:X4}");

            if (full[0] != slaveId) throw new InvalidOperationException($"Unexpected slave id in response: {full[0]}");
            if (full[1] != 0x06) throw new InvalidOperationException($"Unexpected function code in response: {full[1]}");
        }

        #endregion
    }
}

