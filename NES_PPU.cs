using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    public class NES_PPU
    {
        private NES_Cartridge? _cart;
        public byte[,] _tblName = new byte[2, 1024];
        public byte[] _tblPalette = new byte[32];
        public byte[,] _tblPattern = new byte[2, 4096];
        public bool _isFrameComplete = false;
        public bool _NMI_Enable = false;
        public int _scanline = 0;
        public int _cycle = 0;
        private byte _addressLatch = 0; // 1 or 0
        private byte _dataBuffer = 0b0000_0000; // open bus

        public byte _fineX = 0b0000_0000;
        public byte _bgNextTileID = 0b0000_0000;
        public byte _bgNextTileAttrib = 0b0000_0000;
        public byte _bgNextTileLSB = 0b0000_0000;
        public byte _bgNextTileMSB = 0b0000_0000;

        public ushort _bgShifterPatternLo = 0x00;
        public ushort _bgShifterPatternHi = 0x00;
        public ushort _bgShifterAttribLo = 0x00;
        public ushort _bgShifterAttribHi = 0x00;

        public byte _ppuStatus = 0b0000_0000;
        public byte _ppuMask = 0b0000_0000;
        public byte _ppuCtrl = 0b0000_0000;

        public PPU_Addr_Reg _tram = new PPU_Addr_Reg();
        public PPU_Addr_Reg _vram = new PPU_Addr_Reg();

        public uint[] _frameBuffer = new uint[256 * 240];

        // All of the colors the NES is capable of presenting
        static readonly uint[] _nesMasterPalette = new uint[64]
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

        // Purely for demonstration, games are responsible for loading their own palette data
        public void InitializeDefaultPalettes()
        {
            // Palette 0 - Grayscale (classic)
            _tblPalette[0x00] = 0x0F; // Black
            _tblPalette[0x01] = 0x00; // Dark gray  
            _tblPalette[0x02] = 0x10; // Light gray
            _tblPalette[0x03] = 0x30; // White

            // Palette 1 - Blue theme (water/ice)
            _tblPalette[0x04] = 0x0F; // Black
            _tblPalette[0x05] = 0x01; // Deep blue
            _tblPalette[0x06] = 0x11; // Medium blue  
            _tblPalette[0x07] = 0x21; // Light blue

            // Palette 2 - Warm theme (earth/fire) - This is what you're using with _selectedPalette = 0x02
            _tblPalette[0x08] = 0x0F; // Black
            _tblPalette[0x09] = 0x06; // Dark red
            _tblPalette[0x0A] = 0x16; // Orange
            _tblPalette[0x0B] = 0x27; // Yellow

            // Palette 3 - Nature theme (grass/forest)
            _tblPalette[0x0C] = 0x0F; // Black
            _tblPalette[0x0D] = 0x09; // Dark green
            _tblPalette[0x0E] = 0x19; // Medium green
            _tblPalette[0x0F] = 0x29; // Light green

            // Palette 4 - Purple theme (magic/fantasy)
            _tblPalette[0x10] = 0x0F; // Black
            _tblPalette[0x11] = 0x04; // Dark purple
            _tblPalette[0x12] = 0x14; // Medium purple
            _tblPalette[0x13] = 0x24; // Light purple/pink

            // Palette 5 - Sunset theme
            _tblPalette[0x14] = 0x0F; // Black
            _tblPalette[0x15] = 0x08; // Dark orange
            _tblPalette[0x16] = 0x28; // Bright orange
            _tblPalette[0x17] = 0x38; // Light orange/yellow

            // Palette 6 - Ocean theme
            _tblPalette[0x18] = 0x0F; // Black
            _tblPalette[0x19] = 0x02; // Navy blue
            _tblPalette[0x1A] = 0x12; // Ocean blue
            _tblPalette[0x1B] = 0x22; // Sky blue

            // Palette 7 - High contrast (good for detailed sprites)
            _tblPalette[0x1C] = 0x0F; // Black
            _tblPalette[0x1D] = 0x06; // Red
            _tblPalette[0x1E] = 0x2A; // Bright green
            _tblPalette[0x1F] = 0x30; // White
        }

        #region ##### REGISTERS ENUMS #####

        [Flags]
        public enum PPUSTATUS : byte
        {
            UNUSED = 0b0001_1111,
            SPRITE_OVERFLOW = 1 << 5,
            SPRITE_ZERO_HIT = 1 << 6,
            VERTICAL_BLANK = 1 << 7
        };

        [Flags]
        public enum PPUMASK : byte
        {
            GRAYSCALE = 1 << 0,
            RENDER_BG_LEFT = 1 << 1,
            RENDER_SPRITES_LEFT = 1 << 2,
            RENDER_BG = 1 << 3,
            RENDER_SPRITES = 1 << 4,
            ENHANCE_RED = 1 << 5,
            ENHANCE_GREEN = 1 << 6,
            ENHANCE_BLUE = 1 << 7,
        }

        [Flags]
        public enum PPUCTRL : byte
        {
            NAMETABLE_X = 1 << 0,
            NAMETABLE_Y = 1 << 1,
            INCREMENT_MODE = 1 << 2,
            PATTERN_SPRITE = 1 << 3,
            PATTERN_BG = 1 << 4,
            SPRITE_SIZE = 1 << 5,
            SLAVE_MODE = 1 << 6,
            ENABLE_NMI = 1 << 7
        }

        public bool IsSet<TEnum>(TEnum flag, byte reg) where TEnum : Enum
        {
            return (reg & Convert.ToByte(flag)) != 0;
        }

        public void SetFlag<TEnum>(TEnum flag, ref byte reg, bool value) where TEnum : Enum
        {
            byte bFlag = Convert.ToByte(flag);
            if (value)
            {
                reg |= bFlag;
            }
            else
            {
                reg &= (byte)~bFlag;
            }
        }
        #endregion

        public byte CPU_Read(ushort addr)
        {
            byte data = 0x00;

            switch (addr)
            {
                case 0x0000: // Control
                    break;
                case 0x0001: // Mask
                    break;
                case 0x0002: // Status
                    // Combine the bottom five bits of the last PPU transaction with the top three bits of the status register
                    data = (byte)((_ppuStatus & 0xE0) | (_dataBuffer & 0x1F));
                    _ppuStatus &= (byte)~PPUSTATUS.VERTICAL_BLANK; // Unset the vertical blank flag
                    _addressLatch = 0;
                    break;
                case 0x0003: // OAM Address
                    break;
                case 0x0004: // OAM Data
                    break;
                case 0x0005: // Scroll
                    break;
                case 0x0006: // PPU Address
                    break;
                case 0x0007: // PPU Data
                    data = _dataBuffer;
                    _dataBuffer = PPU_Read(_vram._reg);
                    if (_vram._reg >= 0x3F00) // If accessing palette memory, dont wait a clock cycle in the buffer
                    {
                        data = _dataBuffer;
                    }
                    _vram._reg += (_ppuCtrl & (byte)PPUCTRL.INCREMENT_MODE) != 0 ? (byte)32 : (byte)1;
                    break;
            }
            return data;
        }

        public void CPU_Write(ushort addr, byte data)
        {
            switch (addr)
            {
                case 0x0000: // Control
                    _ppuCtrl = data;
                    _tram.NameTableX = (_ppuCtrl & (byte)PPUCTRL.NAMETABLE_X) != 0 ? (byte)1 : (byte)0;
                    _tram.NameTableY = (_ppuCtrl & (byte)PPUCTRL.NAMETABLE_Y) != 0 ? (byte)1 : (byte)0;
                    break;
                case 0x0001: // Mask
                    _ppuMask = data;
                    break;
                case 0x0002: // Status
                    break;
                case 0x0003: // OAM Address
                    break;
                case 0x0004: // OAM Data
                    break;
                case 0x0005: // Scroll
                    if (_addressLatch == 0)
                    {
                        _fineX = (byte)(data & 0x07);
                        _tram.CoarseX = (byte)(data >> 3);
                        _addressLatch = 1;
                    }
                    else
                    {
                        _tram.FineY = (byte)(data & 0x07);
                        _tram.CoarseY = (byte)(data >> 3);
                        _addressLatch = 0;
                    }
                    break;
                case 0x0006: // PPU Address
                    if (_addressLatch == 0)
                    {
                        _tram._reg = (ushort)((_tram._reg & 0x00FF) | ((data & 0x3F) << 8)); // Mask data to 6 bits, shift to high byte
                        _tram._reg &= 0x3FFF; // Turn off Z bit
                        _addressLatch = 1;
                    }
                    else
                    {
                        _tram._reg = (ushort)((_tram._reg & 0xFF00) | data); // Low 8 bits
                        _vram._reg = _tram._reg; // Write temporary address into active vram address
                        _addressLatch = 0;
                    }
                    break;
                case 0x0007: // PPU Data
                    PPU_Write(_vram._reg, data);
                    _vram._reg += (_ppuCtrl & (byte)PPUCTRL.INCREMENT_MODE) != 0 ? (byte)32 : (byte)1;
                    break;
            }
        }

        public byte PPU_Read(ushort addr)
        {

            byte data = 0;
            addr &= 0x3FFF;

            if (_cart != null && _cart.PPU_Read(addr, out data))
            {
                // Cartridge addressing range
            }

            else if (addr >= 0x0000 && addr <= 0x1FFF)
            {
                data = _tblPattern[((addr & 0x1000) >> 12), (addr & 0x0FFF)];
            }

            else if (addr >= 0x2000 && addr <= 0x3EFF)
            {
                // Nametable address range / VRAM
                // TODO: Implement mirroring logic for nametable r/w !!!!! important !!!!! 44:48 OLC tutorial
                // ^ this is kind of done but not sure how I feel about it

                ushort normalizedAddr = (ushort)(addr & 0x0FFF); // Clamp to 4kib range
                byte tableIndex = (byte)(normalizedAddr / 0x0400); // Logical table (0-3)
                ushort tableOffset = (ushort)(normalizedAddr & 0x03FF); // Clamp to 1kib , nameetables are only 1kb each

                // This is only going to work for vertical and horizontal mirroring
                byte physicalTable = (_cart != null && _cart._mirror == NES_Cartridge.Mirror.VERTICAL) ?
                    (byte)(tableIndex & 0x01) // 0,1,0,1 
                  : (byte)(tableIndex >> 1); // 0,0,1,1

                data = _tblName[physicalTable, tableOffset];
            }

            else if (addr >= 0x3F00 && addr <= 0x3FFF)
            {
                addr &= 0x001F;
                if (addr == 0x0010) addr = 0x0000;
                if (addr == 0x0014) addr = 0x0004;
                if (addr == 0x0018) addr = 0x0008;
                if (addr == 0x001C) addr = 0x000C;
                data = (byte)(_tblPalette[addr] & ((_ppuMask & (byte)PPUMASK.GRAYSCALE) != 0 ? 0x30 : 0x3F));
            }

            return data;
        }

        public void PPU_Write(ushort addr, byte data)
        {

            addr &= 0x3FFF;

            if (_cart != null && _cart.PPU_Write(addr, data))
            {

            }

            else if (addr >= 0x0000 && addr <= 0x1FFF)
            {
                _tblPattern[(addr & 0x1000) >> 12, addr & 0x0FFF] = data;
            }

            else if (addr >= 0x2000 && addr <= 0x3EFF)
            {
                ushort normalizedAddr = (ushort)(addr & 0x0FFF); // Clamp to 4kib range
                byte tableIndex = (byte)(normalizedAddr / 0x0400); // Logical table (0-3)
                ushort tableOffset = (ushort)(normalizedAddr & 0x03FF); // Clamp to 1kib , nameetables are only 1kb each

                // This is only going to work for vertical and horizontal mirroring
                byte physicalTable = (_cart != null && _cart._mirror == NES_Cartridge.Mirror.VERTICAL) ?
                    (byte)(tableIndex & 0x01) // 0,1,0,1 
                  : (byte)(tableIndex >> 1); // 0,0,1,1

                _tblName[physicalTable, tableOffset] = data;
            }
            else if (addr >= 0x3F00 && addr <= 0x3FFF)
            {
                addr &= 0x001F;
                if (addr == 0x0010) addr = 0x0000;
                if (addr == 0x0014) addr = 0x0004;
                if (addr == 0x0018) addr = 0x0008;
                if (addr == 0x001C) addr = 0x000C;
                _tblPalette[addr] = data;
            }
        }

        public void IncrementScrollX()
        {
            if ((_ppuMask & (byte)PPUMASK.RENDER_BG) != 0 || (_ppuMask & (byte)PPUMASK.RENDER_SPRITES) != 0)
            {
                if (_vram.CoarseX == 31)
                {
                    _vram.CoarseX = 0;
                    _vram.NameTableX ^= 1;
                }
                else
                {
                    _vram.CoarseX++;
                }
            }
        }

        public void IncrementScrollY()
        {
            if ((_ppuMask & (byte)PPUMASK.RENDER_BG) != 0 || (_ppuMask & (byte)PPUMASK.RENDER_SPRITES) != 0)
            {
                if (_vram.FineY < 7)
                {
                    _vram.FineY++;
                }
                else
                {
                    _vram.FineY = 0;

                    if (_vram.CoarseY == 29)
                    {
                        _vram.CoarseY = 0;
                        _vram.NameTableY ^= 1;
                    }
                    else if (_vram.CoarseY == 31)
                    {
                        _vram.CoarseY = 0;
                    }
                    else
                    {
                        _vram.CoarseY++;
                    }
                }
            }
        }

        public void TransferAddressX()
        {
            if ((_ppuMask & (byte)PPUMASK.RENDER_BG) != 0 || (_ppuMask & (byte)PPUMASK.RENDER_SPRITES) != 0)
            {
                _vram.NameTableX = _tram.NameTableX;
                _vram.CoarseX = _tram.CoarseX;
            }
        }

        public void TransferAddressY()
        {
            if ((_ppuMask & (byte)PPUMASK.RENDER_BG) != 0 || (_ppuMask & (byte)PPUMASK.RENDER_SPRITES) != 0)
            {
                _vram.FineY = _tram.FineY;
                _vram.NameTableY = _tram.NameTableY;
                _vram.CoarseY = _tram.CoarseY;
            }
        }

        public void LoadBGShifters()
        {
            byte nxtAttribLo = (byte)((_bgNextTileAttrib & 0b01) != 0 ? 0xFF : 0x00);
            byte nxtAttribHi = (byte)((_bgNextTileAttrib & 0b10) != 0 ? 0xFF : 0x00);

            _bgShifterPatternLo = (ushort)((_bgShifterPatternLo & 0xFF00) | _bgNextTileLSB);
            _bgShifterPatternHi = (ushort)((_bgShifterPatternHi & 0xFF00) | _bgNextTileMSB);

            _bgShifterAttribLo = (ushort)((_bgShifterAttribLo & 0xFF00) | nxtAttribLo);
            _bgShifterAttribHi = (ushort)((_bgShifterAttribHi & 0xFF00) | nxtAttribHi);
        }

        public void UpdateShifters()
        {
            _bgShifterPatternLo <<= 1;
            _bgShifterPatternHi <<= 1;
            _bgShifterAttribLo <<= 1;
            _bgShifterAttribHi <<= 1;
        }

        public uint[] GetPatternTable(byte index, byte palette)
        {
            // A Pattern Table is a 16x16 grid, where each cell in the grid is 8x8 pixels
            uint[] buffer = new uint[128 * 128];

            // Iterate through each tile in the Pattern Table
            for (ushort tileY = 0; tileY < 16; tileY++)
            {
                for (ushort tileX = 0; tileX < 16; tileX++)
                {
                    ushort offset = (ushort)(tileY * 256 + tileX * 16);

                    // Iterate through each pixel in the grid cell
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
            return _nesMasterPalette[PPU_Read((ushort)(0x3F00 + (palette << 2) + pixel))];
        }

        public void ConnectCartridge(NES_Cartridge cart)
        {
            _cart = cart;
        }

        public void Reset()
        {
            _fineX = 0x00;
            _addressLatch = 0x00;
            _dataBuffer = 0x00;
            _scanline = 0;
            _cycle = 0;
            _bgNextTileID = 0x00;
            _bgNextTileAttrib = 0x00;
            _bgNextTileLSB = 0x00;
            _bgNextTileMSB = 0x00;
            _bgShifterPatternLo = 0x00;
            _bgShifterPatternHi = 0x00;
            _bgShifterAttribLo = 0x00;
            _bgShifterAttribHi = 0x00;
            _ppuStatus = 0b0000_0000;
            _ppuMask= 0b0000_0000;
            _ppuCtrl = 0b0000_0000;
            _vram._reg = 0x00;
            _tram._reg = 0x00;
        }


        public void Clock()
        {

            if (_scanline >= -1 && _scanline < 240)
            {

                if (_scanline == 0 && _cycle == 0)
                {
                    _cycle = 1;
                }


                if (_scanline == -1 && _cycle == 1)
                {
                    _ppuStatus &= (byte)~PPUSTATUS.VERTICAL_BLANK;

                }

                if ((_cycle >= 2 && _cycle < 258) || (_cycle >= 321 && _cycle < 338))
                {
                    UpdateShifters();
                    switch ((_cycle - 1) % 8)
                    {
                        case 0:
                            LoadBGShifters();
                            _bgNextTileID = PPU_Read((ushort)(0x2000 | (_vram._reg & 0x0FFF)));
                            break;
                        case 2:
                            ushort attribAddr = (ushort)((_vram.NameTableY << 11) | (_vram.NameTableX << 10) | ((_vram.CoarseY >> 2) << 3) | (_vram.CoarseX >> 2));
                            _bgNextTileAttrib = PPU_Read((ushort)(0x23C0 | attribAddr));
                            if ((_vram.CoarseY & 0x02) != 0) _bgNextTileAttrib >>= 4;
                            if ((_vram.CoarseX & 0x02) != 0) _bgNextTileAttrib >>= 2;
                            _bgNextTileAttrib &= 0x03;
                            break;
                        case 4:
                            ushort nxtTileLsbAddr = (ushort)((((_ppuCtrl & (byte)PPUCTRL.PATTERN_BG) >> 4) << 12) + (ushort)(_bgNextTileID << 4) + (_vram.FineY + 0));
                            _bgNextTileLSB = PPU_Read(nxtTileLsbAddr);
                            break;
                        case 6:
                            ushort nxtTileMsbAddr = (ushort)((((_ppuCtrl & (byte)PPUCTRL.PATTERN_BG) >> 4) << 12) + (ushort)(_bgNextTileID << 4) + (_vram.FineY + 8));
                            _bgNextTileMSB = PPU_Read(nxtTileMsbAddr);
                            break;
                        case 7:
                            IncrementScrollX();
                            break;
                    }
                }

                if (_cycle == 256)
                {
                    IncrementScrollY();
                }

                if (_cycle == 257)
                {
                    LoadBGShifters();
                    TransferAddressX();
                }

                if (_cycle == 338 || _cycle == 340)
                {
                    _bgNextTileID = PPU_Read((ushort)(0x2000 | (_vram._reg & 0x0FFF)));
                }

                if (_scanline == -1 && _cycle >= 280 && _cycle < 305)
                {
                    TransferAddressY();
                }
            }

            if (_scanline >= 241 && _scanline < 261)
            {
                if (_scanline == 241 && _cycle == 1)
                {
                    _ppuStatus |= (byte)PPUSTATUS.VERTICAL_BLANK;
                    if ((_ppuCtrl & (byte)PPUCTRL.ENABLE_NMI) != 0)
                    {
                        _NMI_Enable = true;
                    }
                }
            }

            byte bg_pixel = 0x00;
            byte bg_palette = 0x00;

            if ((_ppuMask & (byte)PPUMASK.RENDER_BG) != 0)
            {
                ushort bit_mux = (ushort)(0x8000 >> _fineX);

                byte p0_pixel = (_bgShifterPatternLo & bit_mux) > 0 ? (byte)1 : (byte)0;
                byte p1_pixel = (_bgShifterPatternHi & bit_mux) > 0 ? (byte)1 : (byte)0;

                bg_pixel = (byte)((p1_pixel << 1) | p0_pixel);

                byte p0_pal = (_bgShifterAttribLo & bit_mux) > 0 ? (byte)1 : (byte)0;
                byte p1_pal = (_bgShifterAttribHi & bit_mux) > 0 ? (byte)1 : (byte)0;

                bg_palette = (byte)((p1_pal << 1) | p0_pal);
            }

            if (_scanline >= 0 && _scanline < 240 && _cycle > 0 && _cycle <= 256)
            {
                _frameBuffer[_scanline * 256 + (_cycle - 1)] = GetColorFromPaletteRAM(bg_palette, bg_pixel);
            }

            _cycle++;
            if (_cycle >= 341)
            {
                _cycle = 0;
                _scanline++;
                if (_scanline >= 261)
                {
                    _scanline = -1;
                    _isFrameComplete = true;
                }
            }
        }
    }
}
