using NES_Emulator.Tests;

namespace NES_Emulator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //CPU_Tests.RunAllAddressTests();
            //CPU_Tests.RunAll_LDA_Tests();
            //CPU_Tests.RunAllStackTests();
            CPU_Tests.RunAll_STA_Tests();
            //BUS_Tests.RunAllBUSTests();
        }
    }
}
