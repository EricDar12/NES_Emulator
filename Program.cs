
namespace NES_Emulator
{
    internal class Program
    {
        static void Main(string[] args)
        {

            NES_System _nes = new NES_System();
            _nes._bus._cpuRAM[0x0600] = 0xA9;
            _nes._bus._cpuRAM[0x0601] = 0xFF;
            _nes._bus._cpuRAM[0x0602] = 0x0A;
            _nes._bus._cpuRAM[0x0603] = 0x00;
            _nes._cpu._program_counter = 0x0600;
            _nes._cpu.StepOneInstruction();

        }
    }
}
