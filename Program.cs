
namespace NES_Emulator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            NES_Cartridge TestCart = new NES_Cartridge();

            TestCart.LoadNestest("C:\\Users\\eric1\\Documents\\Visual Studio 2022\\Projects\\NES_TestRoms\\nestest.nes");

        }
    }
}
