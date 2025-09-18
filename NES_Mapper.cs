using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    public abstract class NES_Mapper
    {

        public byte _prgBanks;
        public byte _chrBanks;

        public NES_Mapper(byte prgBanks, byte chrBanks)
        {
            _prgBanks = prgBanks;
            _chrBanks = chrBanks;
        }

        public abstract bool CPU_Map_Read(ushort addr, out ushort mappedAddr);
        public abstract bool CPU_Map_Write(ushort addr, out ushort mappedAddr);
        public abstract bool PPU_Map_Read(ushort addr, out ushort mappedAddr);
        public abstract bool PPU_Map_Write(ushort addr, out ushort mappedAddr);

    }
}
