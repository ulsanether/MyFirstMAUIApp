using MauiFirstUartApp.Core.Abstractions;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MauiFirstUartApp.Test;

[TestClass]
public class SerialServiceTests
{
   [TestMethod]
   public async Task GetDeviceNamesAsync_ReturnsDeviceNames()
    {
        var mockSerialService = new Mock<ISerialService>();
        var expectedDeviceNames = new List<string> { "COM1", "COM2", "COM3" };
        mockSerialService.Setup(s => s.GetDeviceNamesAsync())
                           .ReturnsAsync(expectedDeviceNames);

        var deviceNames = await mockSerialService.Object.GetDeviceNamesAsync();

        CollectionAssert.AreEqual(expectedDeviceNames, (System.Collections.ICollection)deviceNames);
    }
    [TestMethod]
    public async Task OpenAsync_Called_With_Parameters()
    {
        // Arrange
        var mock = new Mock<ISerialService>();
        mock.Setup(s => s.OpenAsync("COM1", 9600, 8, 0, 0)).Returns(Task.CompletedTask);

        // Act
        await mock.Object.OpenAsync("COM1", 9600, 8, 0, 0);

        // Assert
        mock.Verify(s => s.OpenAsync("COM1", 9600, 8, 0, 0), Times.Once);
    }

    [TestMethod]
    public async Task WriteAsync_Called()
    {
        // Arrange
        var mock = new Mock<ISerialService>();
        var data = new byte[] { 0x01, 0x02 };
        mock.Setup(s => s.WriteAsync(data, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await mock.Object.WriteAsync(data);

        // Assert
        mock.Verify(s => s.WriteAsync(data, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ReadAsync_Returns_Data()
    {
        // Arrange
        var mock = new Mock<ISerialService>();
        var expected = new byte[] { 0x10, 0x20 };
        mock.Setup(s => s.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        // Act
        var result = await mock.Object.ReadAsync();

        // Assert
        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public async Task CloseAsync_Called()
    {
        // Arrange
        var mock = new Mock<ISerialService>();
        mock.Setup(s => s.CloseAsync()).Returns(Task.CompletedTask);

        // Act
        await mock.Object.CloseAsync();

        // Assert
        mock.Verify(s => s.CloseAsync(), Times.Once);
    }

    [TestMethod]
    public async Task IsConnectedAsync_Returns_True()
    {
        // Arrange
        var mock = new Mock<ISerialService>();
        mock.Setup(s => s.IsConnectedAsync()).ReturnsAsync(true);

        // Act
        var result = await mock.Object.IsConnectedAsync();

        // Assert
        Assert.IsTrue(result);
    }

[TestMethod]

public Task IsModbus_Return_True()
    {
        var mock = new Mock<ISerialService>();

        mock.Setup(s => s.ModbusReadHoldingRegistersAsync(1, 0, 2))
            .ReturnsAsync(new ushort[] { 100, 200 });
        return Task.CompletedTask;
    }



}
