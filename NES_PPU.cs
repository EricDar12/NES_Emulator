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
        private byte[,] _tblName = new byte[2, 1024];
        private byte[] _tblPalette = new byte[32];
        private byte[,] _tblPattern = new byte[2, 4096];
        private bool _isFrameComplete = false;
        private ushort _scanline = 0;
        private ushort _cycle = 0;
        private byte _addressLatch = 0;
        private byte _dataBuffer = 0;
        static readonly uint[] NesMasterPalette = new uint[64]
        {
            0xFF545454, 0xFF001E74, 0xFF081090, 0xFF300088,
            0xFF4C0058, 0xFF580000, 0xFF541800, 0xFF3C1C00,
            0xFF202A00, 0xFF083A00, 0xFF004000, 0xFF003C00,
            0xFF00323C, 0xFF000000, 0xFF000000, 0xFF000000,

            0xFF989698, 0xFF084CC4, 0xFF3032EC, 0xFF5C1EE4,
            0xFF8814B0, 0xFFA01464, 0xFF982220, 0xFF783C00,
            0xFF546400, 0xFF188C00, 0xFF009200, 0xFF008844,
            0xFF006C8C, 0xFF000000, 0xFF000000, 0xFF000000,

            0xFFECEEE4, 0xFF4C9AEC, 0xFF787CEC, 0xFFB062EC,
            0xFFE454EC, 0xFFEC58B4, 0xFFEC6A64, 0xFFD48820,
            0xFFA0AA00, 0xFF74C400, 0xFF4CD020, 0xFF38CC6C,
            0xFF38B4CC, 0xFF3C3C3C, 0xFF000000, 0xFF000000,

            0xFFECEEE4, 0xFFA8CCEC, 0xFFBCBCEC, 0xFFD4B2EC,
            0xFFECAEEC, 0xFFECAED4, 0xFFECAAA8, 0xFFE4C490,
            0xFFCCD278, 0xFFB4DE78, 0xFFA8E290, 0xFF98E2B4,
            0xFFA0D6E4, 0xFFA0A2A0, 0xFF000000, 0xFF000000
        };

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

            if (_cart != null && _cart.PPU_Read(addr, out data))
            {
                // Cartridge addressing range
            }

            else if (addr >= 0x0000 & addr <= 0x1FFF)
            {
                data = _tblPattern[((addr & 0x1000) >> 12), (addr * 0x0FFF)];
            }

            else if (addr >= 0x2000 & addr <= 0x3EFF)
            {

            }

            else if (addr >= 0x3F00 & addr <= 0x3FFF)
            {
                addr &= 0x001F;
                if (addr == 0x0010) addr = 0x0000;
                if (addr == 0x0014) addr = 0x0004;
                if (addr == 0x0018) addr = 0x0008;
                if (addr == 0x001C) addr = 0x000C;
                data = _tblPalette[addr];
            }

            return data;
        }

        public void PPU_Write(ushort addr, byte data)
        {
            if (_cart != null && _cart.PPU_Write(addr, data))
            {

            }

            else if (addr >= 0x0000 & addr <= 0x1FFF)
            {
                _tblPattern[(addr & 0x1000) >> 12, addr * 0x0FFF] = data;
            }

            else if (addr >= 0x2000 & addr <= 0x3EFF)
            {

            }

            else if (addr >= 0x3F00 & addr <= 0x3FFF)
            {
                addr &= 0x001F;
                if (addr == 0x0010) addr = 0x0000;
                if (addr == 0x0014) addr = 0x0004;
                if (addr == 0x0018) addr = 0x0008;
                if (addr == 0x001C) addr = 0x000C;
                _tblPalette[addr] = data;
            }

        }

        public uint[] GetPatternTable(byte index, byte palette)
        {
            uint[] buffer = new uint[128 * 128];

            for (ushort tileY = 0; tileY < 16; tileY++)
            {
                for (ushort tileX = 0; tileX < 16; tileX++)
                {
                    ushort offset = (ushort)(tileY * 256 + tileX * 16);
                    for (ushort row = 0; row < 8; row++)
                    {
                        byte tile_lsb = PPU_Read((ushort)(index * 0x1000 + offset + row + 0));
                        byte tile_msb = PPU_Read((ushort)(index * 0x1000 + offset + row + 8));

                        for (ushort col = 0; col < 8; col++)
                        {
                            byte pixel = (byte)((tile_lsb & 0x01) + (tile_msb & 0x01));

                            tile_lsb >>= 1;
                            tile_msb >>= 1;

                            int x = tileX * 8 + (7 - col);
                            int y = tileY * 8 + row;

                            uint color = GetColorFromPaletteRAM(palette, pixel);

                            buffer[y * 128 + x] = color;
                        }
                    }
                }
            }
            return buffer;
        }

        public uint GetColorFromPaletteRAM(byte palette, byte pixel)
        {
            return NesMasterPalette[(ushort)(PPU_Read((ushort)(0x3F00 + (palette << 2) + pixel)) & 0x3F)];
        }

        public void ConnectCartridge(NES_Cartridge cart)
        {
            _cart = cart;
        }

        public void Reset()
        {
            //throw new NotImplementedException();
        }

        public void Clock()
        {
            //throw new NotImplementedException();
        }
    }
}
