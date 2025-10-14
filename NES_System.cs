using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    public class NES_System
    {
        public NES_BUS _bus;
        public NES_CPU _cpu;
        public NES_PPU _ppu;
        public NES_Cartridge? _cart;
        private int _systemClockCounter = 0;

        public NES_System()
        {
            _ppu = new NES_PPU();
            _bus = new NES_BUS(_ppu);
            _cpu = new NES_CPU(_bus);
        }

        public NES_System(NES_Cartridge cart)
        {
            _ppu = new NES_PPU();
            _bus = new NES_BUS(_ppu);
            _cpu = new NES_CPU(_bus);
            InsertCartridge(cart);
        }

        public void Reset()
        {
            _cpu.Reset();
            _ppu.Reset();
            _systemClockCounter = 0;
        }

        public void Clock()
        {
            _ppu.Clock();
            // PPU runs 3x faster than the CPU
            if (_systemClockCounter % 3 == 0)
            {
                _cpu.Clock();
            }

            if (_ppu._NMI_Enable)
            {
                _ppu._NMI_Enable = false;
                _cpu.NMI();
            }
            _systemClockCounter++;
        }

        public void InsertCartridge(NES_Cartridge cart)
        {
            _cart = cart;
            _bus.ConnectCartridge(_cart);
            _ppu.ConnectCartridge(_cart);
            // Clear system state after insertion
            Reset();
        }

        public string LogPC()
        {
            return Convert.ToString(_cpu._program_counter, 16);
        }
    }
}
