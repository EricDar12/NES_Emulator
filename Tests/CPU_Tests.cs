using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator.Tests
{
    internal class CPU_Tests
    {
        private static void AssertEquals(byte expected, byte actual, string message)
        {
            if (expected != actual) {
                Console.WriteLine($"[FAIL] {message}: Expected {expected}, Got Actual {actual}");
            }
            else {
                Console.WriteLine($"[PASS] {message}");
            }
        }

        public static void TestStatusFlags()
        {

            NES_CPU _cpu = new NES_CPU();

            AssertEquals(0b00000000, _cpu._status, "Inital Values Zero");

            _cpu.SetFlag(NES_CPU.StatusFlags.Negative, true);
            _cpu.SetFlag(NES_CPU.StatusFlags.Zero, true);
            _cpu.SetFlag(NES_CPU.StatusFlags.Carry, true);

            AssertEquals(0b10000011, _cpu._status, "Carry Zero & Negative Set");

            _cpu.SetFlag(NES_CPU.StatusFlags.Negative, false);
            _cpu.SetFlag(NES_CPU.StatusFlags.Zero, false);
            _cpu.SetFlag(NES_CPU.StatusFlags.Carry, false);

            AssertEquals(0b00000000, _cpu._status, "Reset Values");
        }

    }
}
