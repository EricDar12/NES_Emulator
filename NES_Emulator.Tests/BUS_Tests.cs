using Xunit;

namespace NES_Emulator.Tests;
public class BUS_Tests
{
    [Fact]
    public void TestReadWriteByte()
    {
        NES_System nes = new NES_System();
        nes._bus.CPU_Write(0x07FF, 0x0A);
        byte b = nes._bus.CPU_Read(0x07FF);
        Assert.Equal(0x0A, b);
    }
}
