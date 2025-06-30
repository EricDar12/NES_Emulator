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
    }
}
