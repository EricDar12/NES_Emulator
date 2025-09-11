using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    public class NES_Cartridge
    {

        public byte CPU_Read(ushort addr)
        {
            return 0;
        }

        public void CPU_Write(ushort addr, byte data)
        {

        }

        public byte PPU_Read(ushort addr)
        {
            return 0;
        }

        public void PPU_Write(ushort addr, byte data)
        {

        }
    }
}
