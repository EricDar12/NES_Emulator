using NES_Emulator.Tests;

namespace NES_Emulator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CPU_Tests.RunAllCPUTests();
            BUS_Tests.RunAllBUSTests();
        }
    }
}
