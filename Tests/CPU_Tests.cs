using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator.Tests
{
    internal class CPU_Tests
    {
        public static void RunAllCPUTests()
        {
            TestStatusFlags();
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

        public static void TestStatusFlags()
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            Unit_Tests.AssertEquals(0b00000000, cpu._status, "Status Flags Set To Zero");

            cpu.SetFlag(NES_CPU.StatusFlags.Negative, true);
            cpu.SetFlag(NES_CPU.StatusFlags.Zero, true);
            cpu.SetFlag(NES_CPU.StatusFlags.Carry, true);
            cpu.SetFlag(NES_CPU.StatusFlags.Decimal, true);
            cpu.SetFlag(NES_CPU.StatusFlags.Break, true);
            cpu.SetFlag(NES_CPU.StatusFlags.Overflow, true);
            cpu.SetFlag(NES_CPU.StatusFlags.InterruptDisable, true);
            cpu.SetFlag(NES_CPU.StatusFlags.Unused, true);

            Unit_Tests.AssertEquals(0b11111111, cpu._status, "Status Flags Set To One");

            cpu.SetFlag(NES_CPU.StatusFlags.Negative, false);
            cpu.SetFlag(NES_CPU.StatusFlags.Zero, false);
            cpu.SetFlag(NES_CPU.StatusFlags.Carry, false);
            cpu.SetFlag(NES_CPU.StatusFlags.Decimal, false);
            cpu.SetFlag(NES_CPU.StatusFlags.Break, false);
            cpu.SetFlag(NES_CPU.StatusFlags.Overflow, false);
            cpu.SetFlag(NES_CPU.StatusFlags.InterruptDisable, false);
            cpu.SetFlag(NES_CPU.StatusFlags.Unused, false);

            Unit_Tests.AssertEquals(0b00000000, cpu._status, "Status Flags Reset");
        }

        #region ### Addressing Mode Tests ####
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
    }
}
