using NES_Emulator.Tests;

namespace NES_Emulator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //CPU_Tests.RunAllAddressTests();
            CPU_Tests.RunAllInstructionTests();
            //CPU_Tests.RunAllStackTests();
            //BUS_Tests.RunAllBUSTests();
        }
    }
}
