
namespace NES_Emulator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //NES_Cartridge TestCart = new NES_Cartridge();
            //TestCart.LoadNestest("C:\\Users\\eric1\\Documents\\Visual Studio 2022\\Projects\\NES_TestRoms\\nestest.nes");

            // Test Step One Instruction
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);
            bus.WriteByte(0x0007, 0x0E);
            // LDA #80, ASL 80 < 00 (Carry out MSB), ROR ZP 0E > 87 (Carry into MSB), LDX ZP, BRK
            cpu.LoadProgram(new byte[] { 0xA9, 0x80, 0x0A, 0x66, 0x07, 0xA6, 0x07, 0xEA, 0x00 });
            cpu._program_counter = 0x0600;
            cpu.StepOneInstruction();

        }
    }
}
