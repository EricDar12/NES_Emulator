using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator.Tests
{
    internal class CPU_Tests
    {
        public static void RunAllAddressTests()
        {
            TestStatusRegister(); // Should go elsewhere
            TestAddrImmediate();
            TestAddrZeroPage();
            TestAddrZeroPageX();
            TestAddrZeroPageY();
            TestAddrAbsolute();
            TestAddrAbsoluteX();
            TestAddrAbsoluteY();
            TestAddrIndirect();
            TestAddrIndirectX();
            TestAddrIndirectY();
            TestAddrRelative();
        }

        public static void RunAllStackTests()
        {
            TestPushByte();
            TestPopByte();
            TestPushWord();
            TestPopWord();
        }

        public static void RunAllInstructionTests()
        {
            TestLDA();
            TestSTA();
            TestLDX();
            TestSTX();
            TestLDY();
            TestLDY_Absolute();
            TestSTY();
            TestSTY_Absolute();
        }

        #region ##### Instruction Tests #####

        public static void TestLDA()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            // Load accumulator with 65, BRK
            cpu.LoadAndRun(new byte[] {0xA9, 0x65, 0x00});

            Unit_Tests.AssertEquals(0x65, cpu._accumulator, "LDA Imm Loads Correct Value Into A");
            Unit_Tests.AssertEquals(false, cpu.IsFlagSet(NES_CPU.StatusFlags.Zero), "Zero Flag Is Not Set");
            Unit_Tests.AssertEquals(false, cpu.IsFlagSet(NES_CPU.StatusFlags.Negative), "Negative Flag Is Not Set");
            Unit_Tests.AssertEquals(17, (ushort) cpu._master_cycle, "Reset + LDA imm + BRK Uses 17 Cycles");

            Unit_Tests.AssertEquals(0b0011_0000, cpu.PopByte(), "Status Register Pushed & Popped Correctly");
            Unit_Tests.AssertEquals(0b0010_0100, cpu._status, "Value Stored In Status Register Is Correct");
            Unit_Tests.AssertEquals(0x0604, cpu.PopWord(), "Return Address Pushed & Popped Correctlty");

        }

        public static void TestSTA()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            // LDA FF, STA Zero Page with offset 0F, BRK
            cpu.LoadAndRun(new byte[] {0xA9, 0xFF, 0x85, 0x0F, 0x00});

            Unit_Tests.AssertEquals(0xFF, bus.ReadByte(0x000F), "STA Stores Value In Memory Correctly");
            Unit_Tests.AssertEquals(20, (ushort) cpu._master_cycle, "Reset + LDA imm + STA imm + BRK Uses 20 Cycles");

        }

        public static void TestLDX()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            bus.WriteByte(0x000B, 0xA1);

            // LDY 0B, LDX zpy, BRK 
            cpu.LoadAndRun(new byte[] {0xA0, 0x0B, 0xB6, 0x00});

            Unit_Tests.AssertEquals(0xA1, cpu._register_x, "LDX Loads Correct Value From Zero Page (Offset By Y)");
            Unit_Tests.AssertEquals(21, (ushort)cpu._master_cycle, "Reset + LDY imm + LDX zpy + BRK Uses 21 Cycles");
        }

        public static void TestSTX()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            // STX Zero Page set by immediate offset 0xF1, BRK
            cpu.LoadAndRun(new byte[] {0xA2, 0xAA, 0x86, 0xF1, 0x00});

            Unit_Tests.AssertEquals(0xAA, bus.ReadByte(0x00F1), "Register X Correctly Stored In Memory");
            Unit_Tests.AssertEquals(20, (ushort)cpu._master_cycle, "Reset + LDX + STX zpy + BRK uses 20 cycles");
        }

        public static void TestLDY()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);
            bus.WriteByte(0x0085, 0x42);
            // LDX #$05, LDY $80,X, BRK (LDY Zero Page X addressing)
            cpu.LoadAndRun(new byte[] { 0xA2, 0x05, 0xB4, 0x80, 0x00 });
            Unit_Tests.AssertEquals(0x42, cpu._register_y, "LDY Loads Correct Value From Zero Page (Offset By X)");
            Unit_Tests.AssertEquals(21, (ushort)cpu._master_cycle, "Reset + LDX imm + LDY zpx + BRK Uses 21 Cycles");
        }

        public static void TestSTY()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);
            // LDY #$BB, LDX #$10, STY $20,X, BRK (STY Zero Page X addressing)
            cpu.LoadAndRun(new byte[] { 0xA0, 0xBB, 0xA2, 0x10, 0x94, 0x20, 0x00 });
            Unit_Tests.AssertEquals(0xBB, bus.ReadByte(0x0030), "Register Y Correctly Stored In Memory (ZP,X)");
            Unit_Tests.AssertEquals(23, (ushort)cpu._master_cycle, "Reset + LDY imm + LDX imm + STY zpx + BRK uses 23 cycles");
        }

        public static void TestLDY_Absolute()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);
            bus.WriteByte(0x1234, 0x7F);
            // LDY $1234, BRK (LDY Absolute addressing)
            cpu.LoadAndRun(new byte[] { 0xAC, 0x34, 0x12, 0x00 });
            Unit_Tests.AssertEquals(0x7F, cpu._register_y, "LDY Loads Correct Value From Absolute Address");
            Unit_Tests.AssertEquals(19, (ushort)cpu._master_cycle, "Reset + LDY abs + BRK Uses 19 Cycles");
        }

        public static void TestSTY_Absolute()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);
            // LDY #$CC, STY $ABCD, BRK (STY Absolute addressing)
            cpu.LoadAndRun(new byte[] { 0xA0, 0xCC, 0x8C, 0xCD, 0xAB, 0x00 });
            Unit_Tests.AssertEquals(0xCC, bus.ReadByte(0xABCD), "Register Y Correctly Stored At Absolute Address");
            Unit_Tests.AssertEquals(21, (ushort)cpu._master_cycle, "Reset + LDY imm + STY abs + BRK uses 21 cycles");
        }

        #endregion

        #region ##### Stack Tests ##### 

        public static void TestPushByte()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            cpu._stack_pointer = 0xFD;
            ushort expectedAddress = (ushort)(0x0100 + cpu._stack_pointer);

            cpu.PushByte(0x0A);

            Unit_Tests.AssertEquals(0x01FD, expectedAddress, "Expected Address Is Correct");
            Unit_Tests.AssertEquals(0x0A, bus.ReadByte(expectedAddress), "PushByte Correctly Pushes Bytes To The Stack");
            Unit_Tests.AssertEquals(0xFC, cpu._stack_pointer, "PushByte Correctly Decrements Stack Pointer");
        }

        public static void TestPopByte()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            cpu._stack_pointer = 0xFF;

            bus.WriteByte(0x0100, 0x0A);

            byte b = cpu.PopByte(); // FF + 1 = 00 Due to Wrap

            Unit_Tests.AssertEquals(0x00, cpu._stack_pointer, "Pop Byte Wraps Stack Pointer From 0xFF to 0x00");
            Unit_Tests.AssertEquals(0x0A, b, "PopByte Correctly Pops From Wrapped Address");
        }

        public static void TestPushWord()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            cpu._stack_pointer = 0xFD;
            ushort testValue = 0x1234;

            cpu.PushWord(testValue);

            Unit_Tests.AssertEquals(0x12, bus.ReadByte(0x01FD), "PushWord Pushes High Byte Correctly");
            Unit_Tests.AssertEquals(0x34, bus.ReadByte(0x01FC), "PushWord Pushes Low Byte Correctly");
            Unit_Tests.AssertEquals(0xFB, cpu._stack_pointer, "PushWord Correctly Decrements Stack Pointer");
        }

        public static void TestPopWord()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            cpu._stack_pointer = 0xFB;

            bus.WriteByte(0x01FC, 0x34);
            bus.WriteByte(0x01FD, 0x12);

            ushort result = cpu.PopWord();

            Unit_Tests.AssertEquals(0x1234, result, "PopWord Pops Correctly From Stack");
            Unit_Tests.AssertEquals(0xFD, cpu._stack_pointer, "PopWord Correctly Increments Stack Pointer");
        }

        #endregion

        #region ##### Addressing Mode Tests #####
        public static void TestAddrImmediate()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            cpu._program_counter = 0x0100;

            bus.WriteByte(0x0100, 0x44);
            bus.WriteByte(0x0101, 0x34);

            ushort addr = cpu.Addr_Immediate();

            Unit_Tests.AssertEquals(0x0100, addr, "Immediate Address Returns Current PC Value");
            Unit_Tests.AssertEquals(0x44, bus.ReadByte(addr), "Immediate Returns The Correct Byte");
            Unit_Tests.AssertEquals(0x0101, cpu._program_counter, "Immediate Correctly Advances PC By One");
        }

        public static void TestAddrZeroPage()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            cpu._program_counter = 0x0100;

            bus.WriteByte(0x0044, 0xA9);
            bus.WriteByte(0x0100, 0x44);
            bus.WriteByte(0x0101, 0xFF);

            ushort addr = cpu.Addr_ZeroPage();

            Unit_Tests.AssertEquals(0x0044, addr, "Zero Page Returns Correct Address");
            Unit_Tests.AssertEquals(0xA9, bus.ReadByte(addr), "Zero Page Returns Corect Byte");
            Unit_Tests.AssertEquals(0x0101, cpu._program_counter, "Zero Page Correctly Advances PC By One");
        }

        public static void TestAddrZeroPageX()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            cpu._program_counter = 0x0100;
            cpu._register_x = 0x0A;

            bus.WriteByte(0x00DF, 0xFF);
            bus.WriteByte(0x0100, 0xD5); // 0x00D5 + 0x0A = 0x00DF 

            ushort addr = cpu.Addr_ZeroPageX();

            Unit_Tests.AssertEquals(0xDF, addr, "Zero Page X Returns Correct Address");
            Unit_Tests.AssertEquals(0xFF, bus.ReadByte(addr), "Zero Page X Returns Correct Byte");
            Unit_Tests.AssertEquals(0x0101, cpu._program_counter, "Zero Page X Correctly Advances PC By One");
        }

        public static void TestAddrZeroPageY()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            cpu._program_counter = 0x0100;
            cpu._register_y = 0x0A;

            bus.WriteByte(0x0009, 0xA9);
            bus.WriteByte(0x0100, 0xFF); // 0x00FF + 0x0A = 0x0009 due to rollover

            ushort addr = cpu.Addr_ZeroPageY();

            Unit_Tests.AssertEquals(0x0009, addr, "Zero Page Y Returns Correct Address");
            Unit_Tests.AssertEquals(0xA9, bus.ReadByte(addr), "Zero Page Y Returns Correct Byte");
            Unit_Tests.AssertEquals(0x0101, cpu._program_counter, "Zero Page Y Correctly Advances PC By One");
        }

        public static void TestAddrAbsolute()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            cpu._program_counter = 0x0100;
            bus.WriteByte(0x01FF, 0x0A);
            bus.WriteByte(0x0100, 0xFF);
            bus.WriteByte(0x0101, 0x01);

            ushort addr = cpu.Addr_Absolute();

            Unit_Tests.AssertEquals(0x01FF, addr, "Absolute Returns Correct Address");
            Unit_Tests.AssertEquals(0x0A, bus.ReadByte(addr), "Absolute Returns Correct Byte");
            Unit_Tests.AssertEquals(0x0102, cpu._program_counter, "Absolute Correcly Advances PC By Two");
        }

        public static void TestAddrAbsoluteX()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            cpu._program_counter = 0x0100;
            cpu._register_x = 0x05;
            bus.WriteByte(0x0204, 0x0A);
            bus.WriteByte(0x0100, 0xFF);
            bus.WriteByte(0x0101, 0x01);

            ushort addr = cpu.Addr_AbsoluteX();

            Unit_Tests.AssertEquals(0x204, addr, "Absolute X Returns Correct Address");
            Unit_Tests.AssertEquals(0x0A, bus.ReadByte(addr), "Absolute X Returns Correct Byte");
            Unit_Tests.AssertEquals(0x0102, cpu._program_counter, "Absolute X Correcly Advances PC By Two");
        }

        public static void TestAddrAbsoluteY()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            cpu._program_counter = 0x0100;
            cpu._register_y = 0x0A;
            bus.WriteByte(0x0209, 0x0B);
            bus.WriteByte(0x0100, 0xFF);
            bus.WriteByte(0x0101, 0x01);

            ushort addr = cpu.Addr_AbsoluteY();

            Unit_Tests.AssertEquals(0x209, addr, "Absolute Y Returns Correct Address");
            Unit_Tests.AssertEquals(0x0B, bus.ReadByte(addr), "Absolute Y Returns Correct Byte");
            Unit_Tests.AssertEquals(0x0102, cpu._program_counter, "Absolute Y Correcly Advances PC By Two");
        }

        public static void TestAddrIndirect()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            cpu._program_counter = 0x0200;
            bus.WriteByte(0x0200, 0xFF);
            bus.WriteByte(0x0201, 0x03);
            bus.WriteByte(0x03FF, 0x07);
            bus.WriteByte(0x0300, 0x07);
            bus.WriteByte(0x0707, 0x01);

            ushort addr = cpu.Addr_Indirect();

            Unit_Tests.AssertEquals(0x0707, addr, "Indirect Returns Correct Address");
            Unit_Tests.AssertEquals(0x01, bus.ReadByte(addr), "Indirect Returns Correct Byte");
            Unit_Tests.AssertEquals(0x0202, cpu._program_counter, "Indirect Correctly Advances PC By Two");
        }

        public static void TestAddrIndirectX()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            cpu._program_counter = 0x0100;
            cpu._register_x = 0x0005;
            bus.WriteByte(0x0100, 0x0A);
            bus.WriteByte(0x000F, 0x06);
            bus.WriteByte(0x0010, 0x05);
            bus.WriteByte(0x0506, 0x01);

            ushort addr = cpu.Addr_IndirectX();

            Unit_Tests.AssertEquals(0x0506, addr, "Indirect X Returns Correct Address");
            Unit_Tests.AssertEquals(0x01, bus.ReadByte(addr), "Indirect X Returns Correct Byte");
            Unit_Tests.AssertEquals(0x0101, cpu._program_counter, "Indirect X Correctly Advances PC By One");
        }

        public static void TestAddrIndirectY()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            cpu._program_counter = 0x0200;
            cpu._register_y = 0x07;
            bus.WriteByte(0x0200, 0x09);
            bus.WriteByte(0x0009, 0x05);
            bus.WriteByte(0x000A, 0x03);
            bus.WriteByte(0x030C, 0x09);

            ushort addr = cpu.Addr_IndirectY();

            Unit_Tests.AssertEquals(0x030C, addr, "Indirect Y Returns Correct Address");
            Unit_Tests.AssertEquals(0x09, bus.ReadByte(addr), "Indirect Y Returns Correct Byte");
            Unit_Tests.AssertEquals(0x0201, cpu._program_counter, "Indirect Y Correctly Advances PC By One");
        }

        public static void TestAddrRelative()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            cpu._program_counter = 0x0100;
            bus.WriteByte(0x0100, 0x0A);
            bus.WriteByte(0x010B, 0xFF);

            ushort addr = cpu.Addr_Relative();  

            Unit_Tests.AssertEquals(0x010B, addr, "Relative Returns Correct Address");
            Unit_Tests.AssertEquals(0xFF, bus.ReadByte(addr), "Relative Returns Correct Byte");
            Unit_Tests.AssertEquals(0x0101, cpu._program_counter, "Relative Correctly Advances PC By One");
        }
        #endregion

        #region ##### Misc Tests #####

        public static void TestStatusRegister()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            Unit_Tests.AssertEquals(0b0010_0000, cpu._status, "Status Flags Set To Default (Bit 5 On)");

            cpu.SetFlag(NES_CPU.StatusFlags.Negative, true);
            cpu.SetFlag(NES_CPU.StatusFlags.Zero, true);
            cpu.SetFlag(NES_CPU.StatusFlags.Carry, true);
            cpu.SetFlag(NES_CPU.StatusFlags.Decimal, true);
            cpu.SetFlag(NES_CPU.StatusFlags.Break, true);
            cpu.SetFlag(NES_CPU.StatusFlags.Overflow, true);
            cpu.SetFlag(NES_CPU.StatusFlags.InterruptDisable, true);

            Unit_Tests.AssertEquals(0b11111111, cpu._status, "Status Flags Set To One");

            cpu.SetFlag(NES_CPU.StatusFlags.Negative, false);
            cpu.SetFlag(NES_CPU.StatusFlags.Zero, false);
            cpu.SetFlag(NES_CPU.StatusFlags.Carry, false);
            cpu.SetFlag(NES_CPU.StatusFlags.Decimal, false);
            cpu.SetFlag(NES_CPU.StatusFlags.Break, false);
            cpu.SetFlag(NES_CPU.StatusFlags.Overflow, false);
            cpu.SetFlag(NES_CPU.StatusFlags.InterruptDisable, false);

            Unit_Tests.AssertEquals(0b0010_0000, cpu._status, "Status Flags Reset To Default (Bit 5 On)");
        }
        #endregion
    }
}
