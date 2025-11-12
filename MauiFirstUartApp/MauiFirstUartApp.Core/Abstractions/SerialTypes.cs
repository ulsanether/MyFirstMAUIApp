

namespace MauiFirstUartApp.Core.Abstractions;
public enum SerialParity
{
    None,
    Odd,
    Even,
    Mark,
    Space
}

public enum SerialStopBits
{
    None,
    One,
    Two,
    OnePointFive
}
public enum SerialType
{
    Normal,
    Modbus
}

