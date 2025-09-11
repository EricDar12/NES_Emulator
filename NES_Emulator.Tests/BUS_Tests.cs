using Xunit;

namespace NES_Emulator.Tests;
public class BUS_Tests
{
    [Fact]
    public void TestReadWriteByte()
    {
        var bus = new NES_BUS();
        bus.CPU_Write(0x07FF, 0x0A);
        byte b = bus.CPU_Read(0x07FF);
        Assert.Equal(0x0A, b);
    }
}
