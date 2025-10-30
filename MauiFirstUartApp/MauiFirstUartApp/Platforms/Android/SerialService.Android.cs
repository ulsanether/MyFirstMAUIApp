using MauiFirstUartApp.Core.Abstractions;

using UsbSerialForAndroid.Net;
using UsbSerialForAndroid.Net.Drivers;
using UsbSerialForAndroid.Net.Helper;

namespace MauiFirstUartApp.Platforms.Android
{
    public class SerialService : ISerialService
    {
        private UsbDriverBase? _usbDriver;
        private CancellationTokenSource? _modbusPollingCts;
        private readonly SemaphoreSlim _txLock = new SemaphoreSlim(1, 1);

        
        private int _readTimeoutMs = 2000;
       
        private int _postWriteDelayMs = 10;

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
                            var result = await ModbusReadHoldingRegistersAsync(slaveId, startAddress, numberOfPoints);
                            ModbusPolled?.Invoke(result);
                        }
                    }
                    catch
                    {
                        // 필요시 에러 처리/로깅
                    }
                    try
                    {
                        await Task.Delay(pollingIntervalMs, token);
                    }
                    catch (TaskCanceledException) { break; }
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

        // ---------------- Modbus RTU 직접 구현부 ----------------

        // CRC16 (Modbus)
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

        // helper: 요청 전송 후 응답을 읽는 유틸 (헤더/바디를 따로 읽는 방식으로 사용)
        private async Task<byte[]> ReadResponseHeaderAndBodyAsync(int headerLen, Func<byte[], int> getRemainingLengthFromHeader, int timeoutMs)
        {
            if (_usbDriver == null) throw new InvalidOperationException("Serial port not open");

            var buffer = new List<byte>();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // 먼저 headerLen 바이트 수집
            while (buffer.Count < headerLen && sw.ElapsedMilliseconds < timeoutMs)
            {
                var chunk = _usbDriver.Read();
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

            // header 확보 -> 전체 바디 길이 계산
            var headerArr = buffer.Take(headerLen).ToArray();
            int remaining = getRemainingLengthFromHeader(headerArr);
            if (remaining < 0) throw new InvalidOperationException("Invalid header or cannot determine remaining length");

            // 남은 바이트 수집 (remaining includes CRC bytes typically)
            while (buffer.Count < headerLen + remaining && sw.ElapsedMilliseconds < timeoutMs)
            {
                var chunk = _usbDriver.Read();
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

        // Modbus: Read Holding Registers (function 0x03)
        public async Task<ushort[]> ModbusReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            if (_usbDriver == null)
                throw new InvalidOperationException("Serial port not open");

            if (numberOfPoints == 0 || numberOfPoints > 125)
                throw new ArgumentException("Number of points must be between 1 and 125");

            await _txLock.WaitAsync().ConfigureAwait(false);
            try
            {
                // Build request: slave(1) + func(1) + addrHi(1)+addrLo(1) + qtyHi(1)+qtyLo(1) + crcLo(1)+crcHi(1)
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

                // Clear driver's rx buffer if driver supports (no standard here) -> best effort read once
                try { _usbDriver.Read(); } catch { }

                // Write
                _usbDriver.Write(req);

                // small delay to allow device to respond
                await Task.Delay(_postWriteDelayMs).ConfigureAwait(false);

                // First read header 3 bytes: slave, func, byteCount
                byte[] full = await ReadResponseHeaderAndBodyAsync(3, header =>
                {
                    // header[2] == byteCount
                    int byteCount = header[2];
                    // remaining bytes = data(byteCount) + CRC(2)
                    return byteCount + 2;
                }, _readTimeoutMs).ConfigureAwait(false);

                // Validate slave
                if (full.Length < 5) throw new InvalidOperationException("Invalid response length");
                if (full[0] != slaveId) throw new InvalidOperationException($"Unexpected slave id in response: {full[0]}");

                byte func = full[1];
                if ((func & 0x80) != 0)
                {
                    // exception: next byte is exception code, then CRC
                    byte ex = full.Length >= 4 ? full[3] : (byte)0;
                    throw new InvalidOperationException($"Modbus exception from slave {slaveId}: function {(func & 0x7F)}, code {ex}");
                }

                byte byteCountResp = full[2];
                int expectedLen = 3 + byteCountResp + 2; // header + data + CRC
                if (full.Length < expectedLen) throw new InvalidOperationException("Incomplete response");

                // CRC check on (slave..data)
                int payloadLen = 3 + byteCountResp; // exclude CRC
                ushort calcCrc = Crc16(full, payloadLen);
                ushort recvCrc = (ushort)(full[payloadLen] | (full[payloadLen + 1] << 8));
                if (calcCrc != recvCrc) throw new InvalidOperationException($"CRC mismatch. Calc=0x{calcCrc:X4}, Recv=0x{recvCrc:X4}");

                // Parse registers
                int regCount = byteCountResp / 2;
                var regs = new ushort[regCount];
                for (int i = 0; i < regCount; i++)
                {
                    int idx = 3 + i * 2;
                    regs[i] = (ushort)((full[idx] << 8) | full[idx + 1]);
                }

                // Trim returned count to requested numberOfPoints (device might return more/less)
                if (regs.Length != numberOfPoints)
                {
                    var arr = new ushort[numberOfPoints];
                    Array.Copy(regs, 0, arr, 0, Math.Min(regs.Length, numberOfPoints));
                    return arr;
                }

                return regs;
            }
            finally
            {
                _txLock.Release();
            }
        }

        // Modbus: Read Input Registers (function 0x04)
        public async Task<ushort[]> ModbusReadInputRegistersAsync(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            if (_usbDriver == null)
                throw new InvalidOperationException("Serial port not open");

            if (numberOfPoints == 0 || numberOfPoints > 125)
                throw new ArgumentException("Number of points must be between 1 and 125");

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

                try { _usbDriver.Read(); } catch { }

                _usbDriver.Write(req);
                await Task.Delay(_postWriteDelayMs).ConfigureAwait(false);

                byte[] full = await ReadResponseHeaderAndBodyAsync(3, header =>
                {
                    int byteCount = header[2];
                    return byteCount + 2;
                }, _readTimeoutMs).ConfigureAwait(false);

                if (full.Length < 5) throw new InvalidOperationException("Invalid response length");
                if (full[0] != slaveId) throw new InvalidOperationException($"Unexpected slave id in response: {full[0]}");

                byte func = full[1];
                if ((func & 0x80) != 0)
                {
                    byte ex = full.Length >= 4 ? full[3] : (byte)0;
                    throw new InvalidOperationException($"Modbus exception from slave {slaveId}: function {(func & 0x7F)}, code {ex}");
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
            finally
            {
                _txLock.Release();
            }
        }

        // Modbus: Write Single Register (function 0x06)
        public async Task ModbusWriteSingleRegisterAsync(byte slaveId, ushort address, ushort value)
        {
            if (_usbDriver == null)
                throw new InvalidOperationException("Serial port not open");

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

                try { _usbDriver.Read(); } catch { }

                _usbDriver.Write(req);
                await Task.Delay(_postWriteDelayMs).ConfigureAwait(false);

                // Response should echo request (8 bytes)
                // We'll read 8 bytes total
                byte[] full = await ReadResponseHeaderAndBodyAsync(8, header =>
                {
                    // headerLen == 8 means getRemainingLengthFromHeader will not be used; we return 0
                    return 0;
                }, _readTimeoutMs).ConfigureAwait(false);

                if (full.Length < 8) throw new InvalidOperationException("Invalid response length");
                // CRC check on first 6 bytes
                ushort calcCrc = Crc16(full, 6);
                ushort recvCrc = (ushort)(full[6] | (full[7] << 8));
                if (calcCrc != recvCrc) throw new InvalidOperationException($"CRC mismatch. Calc=0x{calcCrc:X4}, Recv=0x{recvCrc:X4}");

                if (full[0] != slaveId) throw new InvalidOperationException($"Unexpected slave id in response: {full[0]}");
                if (full[1] != 0x06) throw new InvalidOperationException($"Unexpected function code in response: {full[1]}");
                // success
            }
            finally
            {
                _txLock.Release();
            }
        }
    }
}