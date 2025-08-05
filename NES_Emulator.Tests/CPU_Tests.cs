using Xunit;

namespace NES_Emulator.Tests;
public class CPU_Tests
{
    [Fact]
    public void TestStatusRegister()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        Assert.Equal(0b0010_0000, cpu._status);

        cpu.SetFlag(NES_CPU.StatusFlags.Negative, true);
        cpu.SetFlag(NES_CPU.StatusFlags.Zero, true);
        cpu.SetFlag(NES_CPU.StatusFlags.Carry, true);
        cpu.SetFlag(NES_CPU.StatusFlags.Decimal, true);
        cpu.SetFlag(NES_CPU.StatusFlags.Break, true);
        cpu.SetFlag(NES_CPU.StatusFlags.Overflow, true);
        cpu.SetFlag(NES_CPU.StatusFlags.InterruptDisable, true);

        Assert.Equal(0b11111111, cpu._status);

        cpu.SetFlag(NES_CPU.StatusFlags.Negative, false);
        cpu.SetFlag(NES_CPU.StatusFlags.Zero, false);
        cpu.SetFlag(NES_CPU.StatusFlags.Carry, false);
        cpu.SetFlag(NES_CPU.StatusFlags.Decimal, false);
        cpu.SetFlag(NES_CPU.StatusFlags.Break, false);
        cpu.SetFlag(NES_CPU.StatusFlags.Overflow, false);
        cpu.SetFlag(NES_CPU.StatusFlags.InterruptDisable, false);

        Assert.Equal(0b0010_0000, cpu._status);
    }

    #region ##### Addressing Mode Tests #####

    [Fact]
    public void AddrImmediate_ReturnsCurrentPCValueAndAdvancesPC()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;

        bus.WriteByte(0x0100, 0x44);
        bus.WriteByte(0x0101, 0x34);

        ushort addr = cpu.Addr_Immediate();

        Assert.Equal(0x0100, addr);
        Assert.Equal(0x44, bus.ReadByte(addr));
        Assert.Equal(0x0101, cpu._program_counter);
    }

    [Fact]
    public void AddrZeroPage_ReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;

        bus.WriteByte(0x0044, 0xA9);
        bus.WriteByte(0x0100, 0x44);

        ushort addr = cpu.Addr_ZeroPage();

        Assert.Equal(0x0044, addr);
        Assert.Equal(0xA9, bus.ReadByte(addr));
        Assert.Equal(0x0101, cpu._program_counter);
    }

    [Fact]
    public void AddrZeroPageX_WrapsAndReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;
        cpu._register_x = 0x0A;

        bus.WriteByte(0x00DF, 0xFF);
        bus.WriteByte(0x0100, 0xD5); // 0xD5 + 0x0A = 0xDF

        ushort addr = cpu.Addr_ZeroPageX();

        Assert.Equal(0x00DF, addr);
        Assert.Equal(0xFF, bus.ReadByte(addr));
        Assert.Equal(0x0101, cpu._program_counter);
    }

    [Fact]
    public void AddrZeroPageY_WrapsAndReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;
        cpu._register_y = 0x0A;

        bus.WriteByte(0x0009, 0xA9);
        bus.WriteByte(0x0100, 0xFF); // 0xFF + 0x0A = 0x09 due to wrap

        ushort addr = cpu.Addr_ZeroPageY();

        Assert.Equal(0x0009, addr);
        Assert.Equal(0xA9, bus.ReadByte(addr));
        Assert.Equal(0x0101, cpu._program_counter);
    }

    [Fact]
    public void AddrAbsolute_ReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;

        bus.WriteByte(0x01FF, 0x0A);
        bus.WriteByte(0x0100, 0xFF);
        bus.WriteByte(0x0101, 0x01);

        ushort addr = cpu.Addr_Absolute();

        Assert.Equal(0x01FF, addr);
        Assert.Equal(0x0A, bus.ReadByte(addr));
        Assert.Equal(0x0102, cpu._program_counter);
    }

    [Fact]
    public void AddrAbsoluteX_ReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;
        cpu._register_x = 0x05;

        bus.WriteByte(0x0204, 0x0A);
        bus.WriteByte(0x0100, 0xFF);
        bus.WriteByte(0x0101, 0x01);

        ushort addr = cpu.Addr_AbsoluteX();

        Assert.Equal(0x0204, addr);
        Assert.Equal(0x0A, bus.ReadByte(addr));
        Assert.Equal(0x0102, cpu._program_counter);
    }

    [Fact]
    public void AddrAbsoluteY_ReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;
        cpu._register_y = 0x0A;

        bus.WriteByte(0x0209, 0x0B);
        bus.WriteByte(0x0100, 0xFF);
        bus.WriteByte(0x0101, 0x01);

        ushort addr = cpu.Addr_AbsoluteY();

        Assert.Equal(0x0209, addr);
        Assert.Equal(0x0B, bus.ReadByte(addr));
        Assert.Equal(0x0102, cpu._program_counter);
    }

    [Fact]
    public void AddrIndirect_EmulatesBugAndReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0200;

        bus.WriteByte(0x0200, 0xFF);
        bus.WriteByte(0x0201, 0x03);
        bus.WriteByte(0x03FF, 0x07); // High byte (bug emulation)
        bus.WriteByte(0x0300, 0x07);
        bus.WriteByte(0x0707, 0x01);

        ushort addr = cpu.Addr_Indirect();

        Assert.Equal(0x0707, addr);
        Assert.Equal(0x01, bus.ReadByte(addr));
        Assert.Equal(0x0202, cpu._program_counter);
    }

    [Fact]
    public void AddrIndirectX_ReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;
        cpu._register_x = 0x05;

        bus.WriteByte(0x0100, 0x0A);
        bus.WriteByte(0x000F, 0x06);
        bus.WriteByte(0x0010, 0x05);
        bus.WriteByte(0x0506, 0x01);

        ushort addr = cpu.Addr_IndirectX();

        Assert.Equal(0x0506, addr);
        Assert.Equal(0x01, bus.ReadByte(addr));
        Assert.Equal(0x0101, cpu._program_counter);
    }

    [Fact]
    public void AddrIndirectY_ReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0200;
        cpu._register_y = 0x07;

        bus.WriteByte(0x0200, 0x09);
        bus.WriteByte(0x0009, 0x05);
        bus.WriteByte(0x000A, 0x03);
        bus.WriteByte(0x030C, 0x09); // 0x0305 + 0x07 = 0x030C

        ushort addr = cpu.Addr_IndirectY();

        Assert.Equal(0x030C, addr);
        Assert.Equal(0x09, bus.ReadByte(addr));
        Assert.Equal(0x0201, cpu._program_counter);
    }

    [Fact]
    public void AddrRelative_ReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;

        bus.WriteByte(0x0100, 0x0B);

        ushort addr = cpu.Addr_Relative();

        Assert.Equal(0x0101, cpu._program_counter);
        Assert.Equal(0x010C, addr);
    }
}

#endregion
