
namespace NES_Emulator
{
    internal class Program
    {
        static void Main(string[] args)
        {

            NES_Cartridge _cart = new NES_Cartridge("C:\\Users\\eric1\\OneDrive\\Documents\\NES_Emulator\\Test ROMs\\Super Mario Bros. (World).nes");    
            NES_System _nes = new NES_System(_cart);
            _nes._cpu.Reset();
            _nes._cpu.StepOneInstruction();

        }
    }
}
