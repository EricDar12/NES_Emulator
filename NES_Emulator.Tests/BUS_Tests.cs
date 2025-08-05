using Xunit;

namespace NES_Emulator.Tests;
public class BUS_Tests
{
    [Fact]
    public void TestReadWriteByte()
    {
        var bus = new NES_BUS();
        bus.WriteByte(0x07FF, 0x0A);
        byte b = bus.ReadByte(0x07FF);
        Assert.Equal(0x0A, b);
    }
}
