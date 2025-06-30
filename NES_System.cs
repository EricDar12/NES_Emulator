using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    internal class NES_System
    {
        public NES_PPU _ppu;
        public NES_CPU _cpu;
        public NES_BUS _bus;

        public NES_System() {
            _ppu = new NES_PPU();
            _bus = new NES_BUS();
            _cpu = new NES_CPU(_bus);
        }

        //public void Step(UInt32 cycle_t_count)
        //{
        //    _master_cycle += cycle_t_count;
        //    _ppu.StepTo(_master_cycle);
        //    _cpu.StepTo(_master_cycle);
        //}

    }
}
