using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    internal class NES_Cartridge
    {

        public void LoadNestest(string path)
        {
            NES_BUS bus = new NES_BUS();
            NES_CPU cpu = new NES_CPU(bus);

            byte[] rom = File.ReadAllBytes(path);

            if (rom[0] != 'N' || rom[1] != 'E' || rom[2] != 'S' || rom[3] != 0x1A)
                throw new Exception("Invalid NES file");

            int prgBanks = rom[4]; // Number of 16KB PRG banks
            int chrBanks = rom[5];

            int prgOffset = 16;
            int prgSize = prgBanks * 16384;
            byte[] prg = new byte[prgSize];
            Array.Copy(rom, prgOffset, prg, 0, prgSize);

            if (prgBanks == 1)
            {
                Array.Copy(prg, 0, bus._memory, 0x8000, 16384);
                Array.Copy(prg, 0, bus._memory, 0xC000, 16384);
            }
            else if (prgBanks == 2)
            {
                Array.Copy(prg, 0, bus._memory, 0x8000, 32768);
            }

            Console.WriteLine("Bytes at $C000: " +
                BitConverter.ToString(bus._memory, 0xC000, 16));

            cpu._program_counter = 0xC000;

            cpu.FetchAndDecode();

            Console.WriteLine($"Test Result code: {bus._memory[0x0002]:X2}");
            Console.WriteLine($"Subcode: {bus._memory[0x0003]:X2}");
        }
    }
}
