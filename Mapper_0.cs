using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    public class Mapper_0 : NES_Mapper
    {

        public Mapper_0(byte prgBanks, byte chrBanks) : base(prgBanks, chrBanks)
        {

        }

        public override bool CPU_Map_Read(ushort addr, out ushort mappedAddr)
        {
            mappedAddr = 0;
            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                // If we have more than 1 prgBank, we know we are working with 32KiB banks
                mappedAddr = (ushort) (addr & (_prgBanks > 1 ? 0x7FFF : 0x3FFF));
                return true;
            }

            return false;
        }

        public override bool CPU_Map_Write(ushort addr, out ushort mappedAddr)
        {
            mappedAddr = 0;
            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                mappedAddr = (ushort)(addr & (_prgBanks > 1 ? 0x7FFF : 0x3FFF));
                return true;
            }

            return false;
        }

        public override bool PPU_Map_Read(ushort addr, out ushort mappedAddr)
        {
            mappedAddr = 0;
            if (addr >= 0x0000 && addr <= 0x1FFF)
            {
                mappedAddr = addr;
                return true;
            }

            return false;
        }

        public override bool PPU_Map_Write(ushort addr, out ushort mappedAddr)
        {
            mappedAddr = 0;
            if (addr >= 0x0000 && addr <= 0x1FFF)
            {
                if (_chrBanks == 0)
                {
                    mappedAddr = addr;
                    return true;
                }
            }
            return false;
        }
    }
}
