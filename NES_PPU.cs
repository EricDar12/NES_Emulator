using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    public class NES_PPU
    {
        private NES_Cartridge? _cart;
        private byte[,] _tblName = new byte[2,1024];
        private byte[] _tblPalette = new byte[32];

        public byte CPU_Read(ushort addr)
        {
            byte data = 0x00;

            switch (addr)
            {
                case 0x0000: 
                    break; // Control
                case 0x0001: 
                    break; // Mask
                case 0x0002: 
                    break; // Status
                case 0x0003: 
                    break; // OAM Address
                case 0x0004: 
                    break; // OAM Data
                case 0x0005: 
                    break; // Scroll
                case 0x0006: 
                    break; // PPU Address
                case 0x0007: 
                    break; // PPU Data
            }
            return data;
        }

        public void CPU_Write(ushort addr, byte data) 
        {
            switch (addr)
            {
                case 0x0000:
                    break; // Control
                case 0x0001:
                    break; // Mask
                case 0x0002:
                    break; // Status
                case 0x0003:
                    break; // OAM Address
                case 0x0004:
                    break; // OAM Data
                case 0x0005:
                    break; // Scroll
                case 0x0006:
                    break; // PPU Address
                case 0x0007:
                    break; // PPU Data
            }
        }

        public byte PPU_Read(ushort addr)
        {

            byte data = 0;

            if (_cart != null && _cart.PPU_Read(addr))
            {
                // Cartridge addressing range
            }

            return data;

        }

        public void PPU_Write(ushort addr, byte data)
        {
            if (_cart != null && _cart.PPU_Write(addr, data))
            {

            }
        }

        public void ConnectCartridge(NES_Cartridge cart)
        {
            _cart = cart;
        }

        public void Reset()
        {

        }

        public void Clock()
        {

        }
    }
}
