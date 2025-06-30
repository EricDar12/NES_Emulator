using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator.Tests
{
    internal class BUS_Tests
    {
        public static void RunAllBUSTests()
        {
            TestReadByte();
            TestWriteByte();
        }
        public static void TestReadByte()
        {
            NES_BUS bus = new NES_BUS();
            bus._memory[0x07FF] = 0x0A;
            byte b = bus.ReadByte(0x07FF);
            Unit_Tests.AssertEquals(0x0A, b, "BUS Correctly Reads Byte At A Given Address");
        }
        public static void TestWriteByte()
        {
            NES_BUS bus = new NES_BUS();
            bus.WriteByte(0x07FF, 0x0A);
            byte b = bus._memory[0x07FF];
            Unit_Tests.AssertEquals(0x0A, b, "BUS Correctly Writes Byte To A Given Address");
        }
    }
}
