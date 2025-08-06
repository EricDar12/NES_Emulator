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

    #region ##### Stack Tests #####
    [Fact]
    public void TestPushByte()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu._stack_pointer = 0xFD;
        ushort expectedAddress = (ushort)(0x0100 + cpu._stack_pointer);

        cpu.PushByte(0x0A);

        Assert.Equal(0x01FD, expectedAddress); // Redundant, but preserved
        Assert.Equal(0x0A, bus.ReadByte(expectedAddress));
        Assert.Equal(0xFC, cpu._stack_pointer);
    }

    [Fact]
    public void TestPopByte()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu._stack_pointer = 0xFF;

        bus.WriteByte(0x0100, 0x0A);

        byte b = cpu.PopByte(); // FF + 1 = 00 due to wrap

        Assert.Equal(0x00, cpu._stack_pointer);
        Assert.Equal(0x0A, b);
    }

    [Fact]
    public void TestPushWord()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu._stack_pointer = 0xFD;
        ushort testValue = 0x1234;

        cpu.PushWord(testValue);

        Assert.Equal(0x12, bus.ReadByte(0x01FD));
        Assert.Equal(0x34, bus.ReadByte(0x01FC));
        Assert.Equal(0xFB, cpu._stack_pointer);
    }

    [Fact]
    public void TestPopWord()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu._stack_pointer = 0xFB;

        bus.WriteByte(0x01FC, 0x34);
        bus.WriteByte(0x01FD, 0x12);

        ushort result = cpu.PopWord();

        Assert.Equal(0x1234, result);
        Assert.Equal(0xFD, cpu._stack_pointer);
    }
    #endregion
    #region ##### Instruction Tests #####

    [Fact]
    public void TestLDA()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu.LoadAndRun(new byte[] { 0xA9, 0x65, 0x00 });

        Assert.Equal(0x65, cpu._accumulator);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.Equal((ushort)17, (ushort)cpu._master_cycle);

        Assert.Equal(0b0011_0000, cpu.PopByte());
        Assert.Equal(0b0010_0100, cpu._status);
        Assert.Equal(0x0604, cpu.PopWord());
    }

    [Fact]
    public void TestSTA()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu.LoadAndRun(new byte[] { 0xA9, 0xFF, 0x85, 0x0F, 0x00 });

        Assert.Equal(0xFF, bus.ReadByte(0x000F));
        Assert.Equal((ushort)20, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestLDX()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        bus.WriteByte(0x000B, 0xA1);

        cpu.LoadAndRun(new byte[] { 0xA0, 0x0B, 0xB6, 0x00 });

        Assert.Equal(0xA1, cpu._register_x);
        Assert.Equal((ushort)21, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestSTX()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu.LoadAndRun(new byte[] { 0xA2, 0xAA, 0x86, 0xF1, 0x00 });

        Assert.Equal(0xAA, bus.ReadByte(0x00F1));
        Assert.Equal((ushort)20, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestLDY()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        bus.WriteByte(0x0085, 0x00);
        cpu.LoadAndRun(new byte[] { 0xA2, 0x05, 0xB4, 0x80, 0x00 });

        Assert.Equal(0x00, cpu._register_y);
        Assert.Equal((ushort)21, (ushort)cpu._master_cycle);
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
    }

    [Fact]
    public void TestSTY()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu.LoadAndRun(new byte[] { 0xA0, 0xBB, 0xA2, 0x10, 0x94, 0x20, 0x00 });

        Assert.Equal(0xBB, bus.ReadByte(0x0030));
        Assert.Equal((ushort)23, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestLDY_Absolute()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        bus.WriteByte(0x1234, 0x7F);
        cpu.LoadAndRun(new byte[] { 0xAC, 0x34, 0x12, 0x00 });

        Assert.Equal(0x7F, cpu._register_y);
        Assert.Equal((ushort)19, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestSTY_Absolute()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu.LoadAndRun(new byte[] { 0xA0, 0xCC, 0x8C, 0xCD, 0xAB, 0x00 });

        Assert.Equal(0xCC, bus.ReadByte(0xABCD));
        Assert.Equal((ushort)21, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestTAX()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu.LoadAndRun(new byte[] { 0xA9, 0x42, 0xAA, 0x00 });

        Assert.Equal(0x42, cpu._register_x);
        Assert.Equal(0x42, cpu._accumulator);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.Equal((ushort)19, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestTXA()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu.LoadAndRun(new byte[] { 0xA2, 0x55, 0x8A, 0x00 });

        Assert.Equal(0x55, cpu._accumulator);
        Assert.Equal(0x55, cpu._register_x);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.Equal((ushort)19, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestTAY()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu.LoadAndRun(new byte[] { 0xA9, 0x33, 0xA8, 0x00 });

        Assert.Equal(0x33, cpu._register_y);
        Assert.Equal(0x33, cpu._accumulator);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.Equal((ushort)19, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestTYA()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu.LoadAndRun(new byte[] { 0xA0, 0xFF, 0x98, 0x00 });

        Assert.Equal(0xFF, cpu._accumulator);
        Assert.Equal(0xFF, cpu._register_y);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.Equal((ushort)19, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestINX()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        byte regX = 0x7E;

        // LDX #$A0, INX, BRK 
        cpu.LoadAndRun(new byte[] { 0xA2, regX, 0xE8, 0x00 });

        Assert.Equal((byte)(regX + 1), cpu._register_x);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.Equal(19, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestDEX()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        byte regX = 0x00;

        // LDX #$00, DEX, BRK
        cpu.LoadAndRun(new byte[] { 0xA2, regX, 0xCA, 0x00 });

        Assert.Equal((byte)(regX - 1), cpu._register_x);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.Equal(19, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestINY()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        byte regY = 0x0A;

        // LDY #$0A, INY, BRK 
        cpu.LoadAndRun(new byte[] { 0xA0, regY, 0xC8, 0x00 });

        Assert.Equal((byte)(regY + 1), cpu._register_y);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.Equal(19, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestDEY()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        byte regY = 0x01;

        // LDY #$09, DEY, BRK
        cpu.LoadAndRun(new byte[] { 0xA0, regY, 0x88, 0x00 });

        Assert.Equal((byte)(regY - 1), cpu._register_x);
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.Equal(19, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestCLC()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // SEC, CLC, BRK
        cpu.LoadAndRun(new byte[] { 0x38, 0x18, 0x00 });
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Carry), "CLC Clears Carry Flag");
        Assert.Equal(19, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestSEC()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // SEC, BRK
        cpu.LoadAndRun(new byte[] { 0x38, 0x00 });
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Carry), "SEC Sets Carry Flag");
        Assert.Equal(17, (ushort)cpu._master_cycle);
    }

    #endregion
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
