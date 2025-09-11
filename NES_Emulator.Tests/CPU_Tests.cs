using System.Buffers;
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
        Assert.Equal(0x0A, bus.CPU_Read(expectedAddress));
        Assert.Equal(0xFC, cpu._stack_pointer);
    }

    [Fact]
    public void TestPopByte()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu._stack_pointer = 0xFF;

        bus.CPU_Write(0x0100, 0x0A);

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

        Assert.Equal(0x12, bus.CPU_Read(0x01FD));
        Assert.Equal(0x34, bus.CPU_Read(0x01FC));
        Assert.Equal(0xFB, cpu._stack_pointer);
    }

    [Fact]
    public void TestPopWord()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu._stack_pointer = 0xFB;

        bus.CPU_Write(0x01FC, 0x34);
        bus.CPU_Write(0x01FD, 0x12);

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

        Assert.Equal(0xFF, bus.CPU_Read(0x000F));
        Assert.Equal((ushort)20, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestLDX()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        bus.CPU_Write(0x000B, 0xA1);

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

        Assert.Equal(0xAA, bus.CPU_Read(0x00F1));
        Assert.Equal((ushort)20, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestLDY()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        bus.CPU_Write(0x0085, 0x00);
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

        Assert.Equal(0xBB, bus.CPU_Read(0x0030));
        Assert.Equal((ushort)23, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestLDY_Absolute()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        bus.CPU_Write(0x1234, 0x7F);
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

        Assert.Equal(0xCC, bus.CPU_Read(0xABCD));
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

    [Fact]
    public void TestJMP_ABS()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // JMP Target
        bus.CPU_Write(0x0107, 0xA9);
        bus.CPU_Write(0x0108, 0x0C);
        bus.CPU_Write(0x0109, 0x00);

        // JMP ABS, 0x0107, LDA IMM, BRK
        cpu.LoadAndRun(new byte[] { 0x4C, 0x07, 0x01 });

        Assert.Equal(0x0C, cpu._accumulator);
        Assert.Equal(20, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestJMP_Indirect()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // JMP Target
        bus.CPU_Write(0x0209, 0x0C);
        bus.CPU_Write(0x020A, 0x03);
        bus.CPU_Write(0x030C, 0xA9);
        bus.CPU_Write(0x030D, 0x01);
        bus.CPU_Write(0x030E, 0x00);

        cpu.LoadAndRun(new byte[] { 0x6C, 0x09, 0x02 });

        Assert.Equal(0x01, cpu._accumulator);
        Assert.Equal(22, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestJSR_Absolute()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        bus.CPU_Write(0x030D, 0x00);

        cpu.LoadAndRun(new byte[] { 0x20, 0x0D, 0x03 });

        Assert.Equal(0b0011_0000, cpu.PopByte()); // SR Pushed To Stack
        Assert.Equal(0x030F, cpu.PopWord()); // BRK Address Pushed To Stack
        Assert.Equal(0x0602, cpu.PopWord()); // JSR Address Pushed To Stack
        Assert.Equal(21, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestRTS()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        bus.CPU_Write(0x0107, 0xA9); // LDA FF
        bus.CPU_Write(0x0108, 0x0F);
        bus.CPU_Write(0x0109, 0x60); // RTS, BRK

        cpu.LoadAndRun(new byte[] { 0x20, 0x07, 0x01, 0x00 });

        Assert.Equal(0x0F, cpu._accumulator);
        Assert.Equal(0b0011_0000, cpu.PopByte());
        Assert.Equal(0x0605, cpu.PopWord()); // BRK Reached
    }

    [Fact]
    public void TestBEQ()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // Branch Target
        bus.CPU_Write(0x0584, 0xA9);
        bus.CPU_Write(0x0585, 0x0A);
        bus.CPU_Write(0x0586, 0x00);

        // LDA #0, BEQ 0x0604 - 0x80, BRK
        cpu.LoadAndRun(new byte[] { 0xA9, 0x00, 0xF0, 0x80 });

        Assert.Equal(0x0A, cpu._accumulator);
        Assert.Equal(23, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestBNE()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // Branch Target
        bus.CPU_Write(0x0683, 0xA9);
        bus.CPU_Write(0x0684, 0x0C);
        bus.CPU_Write(0x0685, 0x00);

        // LDA #01, BNE 0x0604 + 0x07F, LDA #0C, BRK
        cpu.LoadAndRun(new byte[] { 0xA9, 0x01, 0xD0, 0x7F });

        Assert.Equal(0x0C, cpu._accumulator);
        Assert.Equal(22, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestBCC()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // Branch Target
        bus.CPU_Write(0x05F9, 0xA9);
        bus.CPU_Write(0x05FA, 0x0B);
        bus.CPU_Write(0x05FB, 0x00);

        // CLC, BCC 0x0603 - 0x0A, LDA #0B, BRK
        cpu.LoadAndRun(new byte[] { 0x18, 0x90, 0xF6 });

        Assert.Equal(0x0B, cpu._accumulator);
        Assert.Equal(23, (ushort) cpu._master_cycle);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Carry));
    }

    [Fact]
    public void TestBCS()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // Branch Target
        bus.CPU_Write(0x060D, 0xA9);
        bus.CPU_Write(0x060E, 0x0B);
        bus.CPU_Write(0x060F, 0x00);

        // SEC, BCS 0x0603 + 0x0A, LDA #0B, BRK
        cpu.LoadAndRun(new byte[] { 0x38, 0xB0, 0x0A });

        Assert.Equal(0x0B, cpu._accumulator);
        Assert.Equal(22, (ushort)cpu._master_cycle);
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Carry));
    }

    [Fact]
    public void TestBPL()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // Branch Target
        bus.CPU_Write(0x0618, 0xA0);
        bus.CPU_Write(0x0619, 0x0C);
        bus.CPU_Write(0x061A, 0x00);

        // LDA #7F, BPL 0x0604 + 0x14, LDY #0C, BRK 
        cpu.LoadAndRun(new byte[] { 0xA9, 0x7F, 0x10, 0x14 });

        Assert.Equal(0x0C, cpu._register_y);
        Assert.Equal(22, (ushort)cpu._master_cycle);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
    }

    [Fact]
    public void TestBMI()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // Branch Target
        bus.CPU_Write(0x0584, 0xA2);
        bus.CPU_Write(0x0585, 0x0C);
        bus.CPU_Write(0x0586, 0x00);

        // LDA #F4, BMI 0x0604 + 0x14, LDY #0C, BRK 
        cpu.LoadAndRun(new byte[] { 0xA9, 0xF4, 0x30, 0x80 });

        Assert.Equal(0x0C, cpu._register_x);
        Assert.Equal(23, (ushort)cpu._master_cycle);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
    }

    [Fact]
    public void TestADC()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // IndirectX Target
        bus.CPU_Write(0x004E, 0x41);
        bus.CPU_Write(0x004F, 0x02);

        // Pointer Address
        bus.CPU_Write(0x0241, 0x0A);

        // LDA #0A, LDX #44, ADC (Ind X) 0A, BRK
        cpu.LoadAndRun(new byte[] { 0xA9, 0x0A, 0xA2, 0x44, 0x61, 0x0A, 0x00 });

        Assert.Equal(0x14, cpu._accumulator);
        Assert.Equal(25, (ushort) cpu._master_cycle);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Carry));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Overflow));
    }

    [Fact]
    public void TestADC_CarryAndOverflow()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // LDA #-128, ADC Imm # -128 + -1 = -129 (overflow set)
        // 1000 0000 + 1111 1111 = 1 0111 1111 (msb discarded, result is 127) 
        cpu.LoadAndRun(new byte[] { 0xA9, 0x80, 0x69, 0xFF, 0x00 });

        Assert.Equal(0x7F, cpu._accumulator);
        Assert.Equal(19, (ushort)cpu._master_cycle);
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Carry));
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Overflow));
    }

    [Fact]
    public void TestADC_OverflowNoCarry()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // LDA #127, ADC #1
        cpu.LoadAndRun(new byte[] { 0xA9, 0x7F, 0x69, 0x01, 0x00});

        Assert.Equal(0x80, cpu._accumulator); // -128 in signed
        Assert.Equal(19, (ushort) cpu._master_cycle);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Carry));
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Overflow));
    }

    [Fact]
    public void TestSBC()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // SEC, LDA #0, SBC #01
        cpu.LoadAndRun(new byte[] { 0x38, 0xA9, 0x00, 0xE9, 0x01, 0x00});

        Assert.Equal(0xFF, cpu._accumulator);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Carry));
        Assert.Equal(21, (ushort) cpu._master_cycle);
    }

    [Fact]
    public void TestSBC_NoBorrow()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // SEC, LDA #$05, SBC #$03, BRK
        cpu.LoadAndRun(new byte[] { 0x38, 0xA9, 0x05, 0xE9, 0x03, 0x00 });

        // 5 - 3 = 2
        Assert.Equal(0x02, cpu._accumulator);
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Carry));   // no borrow needed, carry set
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.Equal(21, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestCMP()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // LDA #05, CMP Imm 05 - 03, BRK
        cpu.LoadAndRun(new byte[] { 0xA9, 0x05, 0xC9, 0x03, 0x00 });

        Assert.Equal(0x05, cpu._accumulator);
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Carry));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.Equal(19, (ushort) cpu._master_cycle);
    }

    [Fact]
    public void TestCPX()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // LDX #0A, CPX Imm 10 - 10, BRK
        cpu.LoadAndRun(new byte[] { 0xA2, 0x0A, 0xE0, 0x0A, 0x00 });

        Assert.Equal(0x0A, cpu._register_x);
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Carry));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.Equal(19, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestCPY()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // LDY #0A, CPY Imm 10 - 12, BRK
        cpu.LoadAndRun(new byte[] { 0xA0, 0x0A, 0xE0, 0x0C, 0x00 });

        Assert.Equal(0x0A, cpu._register_y);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Carry));
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.Equal(19, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestAND()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        bus.CPU_Write(0x000F, 0x09);

        // LDA #05, LDX #0F, AND Zpx (5 AND 9), BRK 
        cpu.LoadAndRun(new byte[] { 0xA9, 0x05, 0xA2, 0x0F, 0x35, 0x00});

        Assert.Equal(0x01, cpu._accumulator);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.Equal(23, (ushort) cpu._master_cycle);
    }

    [Fact]
    public void TestORA()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu.LoadAndRun(new byte[] { 0xA9, 0x0C, 0x09, 0xFF, 0x00 });

        Assert.Equal(0xFF, cpu._accumulator);
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.Equal(19, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestEOR()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu.LoadAndRun(new byte[] { 0xA9, 0x0C, 0x49, 0x0C, 0x00 });

        Assert.Equal(0x00, cpu._accumulator);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.Equal(19, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestASL_Implied()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // LDA #82, ASL Implied, BRK
        cpu.LoadAndRun(new byte[] { 0xA9, 0x82, 0x0A, 0x00 });

        Assert.Equal(0x04, cpu._accumulator);
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Carry));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.Equal(19, (ushort) cpu._master_cycle);
    }

    [Fact]
    public void TestASL()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        bus.CPU_Write(0x0007, 0x0C);

        // ASL ZP (0000 + 7), LDY ZP (0000 + 7)
        cpu.LoadAndRun(new byte[] { 0x06, 0x07, 0xA4, 0x07, 0x00 });

        Assert.Equal(0x18, cpu._register_y);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Carry));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.Equal(23, (ushort) cpu._master_cycle);
    }

    [Fact]
    public void TestLSR()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // LDA #05, LSR 0101 > 0010, LSR 0010 > 0001 BRK
        cpu.LoadAndRun(new byte[] { 0xA9, 0x05, 0x4A, 0x4A, 0x00 });

        Assert.Equal(0x01, cpu._accumulator);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Carry));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.Equal(21, (ushort) cpu._master_cycle);
    }

    [Fact]
    public void TestROL()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // LDA #87, ROL 1000 0000 < 0000 0000, BRK
        cpu.LoadAndRun(new byte[] { 0xA9, 0x80, 0x2A, 0x00 });

        Assert.Equal(0x00, cpu._accumulator);
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Carry));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.Equal(19, (ushort) cpu._master_cycle);
    }

    [Fact]
    public void TestROR()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        bus.CPU_Write(0x0007, 0x0E);

        // LDA #80, ASL 80 < 00 (Carry out MSB), ROR ZP 0E > 87 (Carry into MSB), LDX ZP, BRK
        cpu.LoadAndRun(new byte[] { 0xA9, 0x80, 0x0A, 0x66, 0x07, 0xA6, 0x07, 0x00 });

        Assert.Equal(0x00, cpu._accumulator);
        Assert.Equal(0x87, cpu._register_x);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Carry));
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.Equal(27, (ushort)cpu._master_cycle);
    }

    [Fact]
    public void TestIRQ()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu._program_counter = 0x0200;

        // Write IRQ At Reset Vector
        bus.CPU_Write(0xFFFE, 0x00); 
        bus.CPU_Write(0xFFFF, 0x03);

        // Program To Be Executed After RTI
        bus.CPU_Write(0x0200, 0xA2);    
        bus.CPU_Write(0x0201, 0x01);    
        bus.CPU_Write(0x0202, 0x00);    

        // Service Routine
        bus.CPU_Write(0x0300, 0xA9); 
        bus.CPU_Write(0x0301, 0xFF);
        bus.CPU_Write(0x0302, 0x40);

        // Set PC To 0x0300
        cpu.IRQ(); 
        cpu.FetchAndDecode();

        Assert.Equal(0xFF, cpu._accumulator); // ISR Executed
        Assert.Equal(0x01, cpu._register_x); // Return and continue execution
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Break));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.InterruptDisable));
    }

    [Fact]
    public void TestNMI()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        cpu._program_counter = 0x0200;
        
        // NMI Should Still Fire
        cpu.SetFlag(NES_CPU.StatusFlags.InterruptDisable, true); 

        // NMI Vector
        bus.CPU_Write(0xFFFA, 0x00);
        bus.CPU_Write(0xFFFB, 0x03);

        // Reset Vector (For BRK)
        bus.CPU_Write(0xFFFE, 0x00);
        bus.CPU_Write(0xFFFF, 0x04);

        // Service Routine
        bus.CPU_Write(0x0300, 0xA9);
        bus.CPU_Write(0x0301, 0x01);
        bus.CPU_Write(0x0302, 0x40);

        // Program To Be Executed After RTI
        bus.CPU_Write(0x0200, 0xA0);
        bus.CPU_Write(0x0201, 0xFF);
        bus.CPU_Write(0x0203, 0x00);

        // Set PC To 0x0300
        cpu.NMI();
        cpu.FetchAndDecode();

        Assert.Equal(0x01, cpu._accumulator);
        Assert.Equal(0xFF, cpu._register_y);
        Assert.Equal(0x0400, cpu._program_counter);
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Break));
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.InterruptDisable));
    }

    [Fact]
    public void TestSEI_CLI_CLV()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        // No opcode exists to manually set overflow
        cpu.SetFlag(NES_CPU.StatusFlags.Overflow, true);

        cpu.LoadAndRun(new byte[] { 0x78, 0xB8, 0x00 });

        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.InterruptDisable));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Overflow));
        Assert.Equal(19, (ushort)cpu._master_cycle);
    }

    [Fact]
    public static void TestBIT()
    {
        NES_BUS bus = new NES_BUS();
        NES_CPU cpu = new NES_CPU(bus);

        bus.CPU_Write(0x000A, 0b0100_0000);

        cpu.LoadAndRun(new byte[] { 0xA9, 0xFF, 0x24, 0x0A, 0x00});

        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Negative));
        Assert.False(cpu.IsFlagSet(NES_CPU.StatusFlags.Zero));
        Assert.True(cpu.IsFlagSet(NES_CPU.StatusFlags.Overflow));
    }

    #endregion
    #region ##### Addressing Mode Tests #####
    [Fact]
    public void AddrImmediate_ReturnsCurrentPCValueAndAdvancesPC()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;

        bus.CPU_Write(0x0100, 0x44);
        bus.CPU_Write(0x0101, 0x34);

        ushort addr = cpu.Addr_Immediate();

        Assert.Equal(0x0100, addr);
        Assert.Equal(0x44, bus.CPU_Read(addr));
        Assert.Equal(0x0101, cpu._program_counter);
    }

    [Fact]
    public void AddrZeroPage_ReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;

        bus.CPU_Write(0x0044, 0xA9);
        bus.CPU_Write(0x0100, 0x44);

        ushort addr = cpu.Addr_ZeroPage();

        Assert.Equal(0x0044, addr);
        Assert.Equal(0xA9, bus.CPU_Read(addr));
        Assert.Equal(0x0101, cpu._program_counter);
    }

    [Fact]
    public void AddrZeroPageX_WrapsAndReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;
        cpu._register_x = 0x0A;

        bus.CPU_Write(0x00DF, 0xFF);
        bus.CPU_Write(0x0100, 0xD5); // 0xD5 + 0x0A = 0xDF

        ushort addr = cpu.Addr_ZeroPageX();

        Assert.Equal(0x00DF, addr);
        Assert.Equal(0xFF, bus.CPU_Read(addr));
        Assert.Equal(0x0101, cpu._program_counter);
    }

    [Fact]
    public void AddrZeroPageY_WrapsAndReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;
        cpu._register_y = 0x0A;

        bus.CPU_Write(0x0009, 0xA9);
        bus.CPU_Write(0x0100, 0xFF); // 0xFF + 0x0A = 0x09 due to wrap

        ushort addr = cpu.Addr_ZeroPageY();

        Assert.Equal(0x0009, addr);
        Assert.Equal(0xA9, bus.CPU_Read(addr));
        Assert.Equal(0x0101, cpu._program_counter);
    }

    [Fact]
    public void AddrAbsolute_ReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;

        bus.CPU_Write(0x01FF, 0x0A);
        bus.CPU_Write(0x0100, 0xFF);
        bus.CPU_Write(0x0101, 0x01);

        ushort addr = cpu.Addr_Absolute();

        Assert.Equal(0x01FF, addr);
        Assert.Equal(0x0A, bus.CPU_Read(addr));
        Assert.Equal(0x0102, cpu._program_counter);
    }

    [Fact]
    public void AddrAbsoluteX_ReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;
        cpu._register_x = 0x05;

        bus.CPU_Write(0x0204, 0x0A);
        bus.CPU_Write(0x0100, 0xFF);
        bus.CPU_Write(0x0101, 0x01);

        ushort addr = cpu.Addr_AbsoluteX();

        Assert.Equal(0x0204, addr);
        Assert.Equal(0x0A, bus.CPU_Read(addr));
        Assert.Equal(0x0102, cpu._program_counter);
    }

    [Fact]
    public void AddrAbsoluteY_ReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;
        cpu._register_y = 0x0A;

        bus.CPU_Write(0x0209, 0x0B);
        bus.CPU_Write(0x0100, 0xFF);
        bus.CPU_Write(0x0101, 0x01);

        ushort addr = cpu.Addr_AbsoluteY();

        Assert.Equal(0x0209, addr);
        Assert.Equal(0x0B, bus.CPU_Read(addr));
        Assert.Equal(0x0102, cpu._program_counter);
    }

    [Fact]
    public void AddrIndirect_EmulatesBugAndReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0200;

        bus.CPU_Write(0x0200, 0xFF);
        bus.CPU_Write(0x0201, 0x03);
        bus.CPU_Write(0x03FF, 0x07); // High byte (bug emulation)
        bus.CPU_Write(0x0300, 0x07);
        bus.CPU_Write(0x0707, 0x01);

        ushort addr = cpu.Addr_Indirect();

        Assert.Equal(0x0707, addr);
        Assert.Equal(0x01, bus.CPU_Read(addr));
        Assert.Equal(0x0202, cpu._program_counter);
    }

    [Fact]
    public void AddrIndirectX_ReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;
        cpu._register_x = 0x05;

        bus.CPU_Write(0x0100, 0x0A);
        bus.CPU_Write(0x000F, 0x06);
        bus.CPU_Write(0x0010, 0x05);
        bus.CPU_Write(0x0506, 0x01);

        ushort addr = cpu.Addr_IndirectX();

        Assert.Equal(0x0506, addr);
        Assert.Equal(0x01, bus.CPU_Read(addr));
        Assert.Equal(0x0101, cpu._program_counter);
    }

    [Fact]
    public void AddrIndirectY_ReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0200;
        cpu._register_y = 0x07;

        bus.CPU_Write(0x0200, 0x09);
        bus.CPU_Write(0x0009, 0x05);
        bus.CPU_Write(0x000A, 0x03);
        bus.CPU_Write(0x030C, 0x09); // 0x0305 + 0x07 = 0x030C

        ushort addr = cpu.Addr_IndirectY();

        Assert.Equal(0x030C, addr);
        Assert.Equal(0x09, bus.CPU_Read(addr));
        Assert.Equal(0x0201, cpu._program_counter);
    }

    [Fact]
    public void AddrRelative_ReturnsCorrectAddress()
    {
        var bus = new NES_BUS();
        var cpu = new NES_CPU(bus);
        cpu._program_counter = 0x0100;

        bus.CPU_Write(0x0100, 0x0B);

        ushort addr = cpu.Addr_Relative();

        Assert.Equal(0x0101, cpu._program_counter);
        Assert.Equal(0x010C, addr);
    }
}
#endregion
