using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    public class NES_PPU
    {
        public NES_Cartridge? _cart;
        public byte[,] _tblName = new byte[2, 1024];
        public byte[] _tblPalette = new byte[32];
        public byte[,] _tblPattern = new byte[2, 4096];

        public bool _isFrameComplete = false;
        public bool _NMI_Enable = false;
        public int _scanline = 0;
        public int _dot = 0;
        private byte _addressLatch = 0; // 1 or 0
        private byte _dataBuffer = 0b0000_0000;

        private byte _fineX = 0b0000_0000;
        private byte _bgNextTileID = 0b0000_0000;
        private byte _bgNextTileAttrib = 0b0000_0000;
        private byte _bgNextTileLSB = 0b0000_0000;
        private byte _bgNextTileMSB = 0b0000_0000;

        private ushort _bgShifterPatternLo = 0x00;
        private ushort _bgShifterPatternHi = 0x00;
        private ushort _bgShifterAttribLo = 0x00;
        private ushort _bgShifterAttribHi = 0x00;

        public byte[] _ppuOAM = new byte[256]; // 64 four byte entries
        public byte[] _scanlineOAM = new byte[32]; // A maximum of 8 sprites to be rendered this scanline
        private byte _oamIndex = 0;
        private bool _spriteZeroHitPossible = false;
        private bool _spriteZeroRendering = false;  
        private byte _spriteCount = 0;
        public Span<OAMEntry> OAMEntries => MemoryMarshal.Cast<byte, OAMEntry>(_ppuOAM);
        public Span<OAMEntry> ScanlineOAM => MemoryMarshal.Cast<byte, OAMEntry>(_scanlineOAM);

        private byte[] _spriteShifterPatternLo = new byte[8];
        private byte[] _spriteShifterPatternHi = new byte[8];
        
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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OAMEntry()
        {
            public byte y;
            public byte id;
            public byte attrib;
            public byte x;
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
                    _dataBuffer = PPU_Read(_vram.Reg);
                    if (_vram.Reg >= 0x3F00) // If accessing palette memory, dont wait a clock cycle in the buffer
                    {
                        data = _dataBuffer;
                    }
                    _vram.Reg += (byte)((_ppuCtrl & (byte)PPUCTRL.INCREMENT_MODE) != 0 ? 32 : 1);
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
                    _tram.NameTableX = (byte)((_ppuCtrl & (byte)PPUCTRL.NAMETABLE_X) != 0 ? 1 : 0);
                    _tram.NameTableY = (byte)((_ppuCtrl & (byte)PPUCTRL.NAMETABLE_Y) != 0 ? 1 : 0);
                    break;;
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
                        _tram.Reg = (ushort)(((data & 0x3F) << 8) | _tram.Reg & 0x00FF);
                        _addressLatch = 1;
                    }
                    else
                    {
                        _tram.Reg = (ushort)((_tram.Reg & 0xFF00) | data); // Low 8 bits
                        _vram.Reg = _tram.Reg;
                        _addressLatch = 0;

                    }
                    break;
                case 0x0007: // PPU Data
                    PPU_Write(_vram.Reg, data);
                    _vram.Reg += (byte)((_ppuCtrl & (byte)PPUCTRL.INCREMENT_MODE) != 0 ? 32 : 1);
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
                // TODO: Implement mirroring logic for nametable r/w !!!!! important !!!!! 44:48 OLC tutorial pt 4
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
                ushort normalizedAddr = (ushort)(addr & 0x0FFF);
                byte tableIndex = (byte)(normalizedAddr / 0x0400); 
                ushort tableOffset = (ushort)(normalizedAddr & 0x03FF); 

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

        // OLC (Javidx9) was a significant help in understanding and implementing PPU frame timing in this Clock function.
        public void Clock()
        {
            if (_scanline >= -1 && _scanline < 240)
            {
                // Background Rendering //
                if (_scanline == 0 && _dot == 0)
                {
                    _dot = 1;
                }

                if (_scanline == -1 && _dot == 1)
                {
                    // New frame, unset vertical blank, sprite overflow, and zero hit flags
                    _ppuStatus &= (byte)~PPUSTATUS.VERTICAL_BLANK;
                    _ppuStatus &= (byte)~PPUSTATUS.SPRITE_OVERFLOW;
                    _ppuStatus &= (byte)~PPUSTATUS.SPRITE_ZERO_HIT;
                    ClearSpriteShifters();
                }

                if ((_dot >= 2 && _dot < 258) || (_dot >= 321 && _dot < 338))
                {
                    UpdateShifters();
                    switch ((_dot - 1) % 8)
                    {
                        case 0:
                            LoadBGShifters();
                            _bgNextTileID = PPU_Read((ushort)(0x2000 | (_vram.Reg & 0x0FFF)));
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

                if (_dot == 256)
                {
                    IncrementScrollY();
                }

                if (_dot == 257)
                {
                    LoadBGShifters();
                    TransferAddressX();
                }

                if (_dot == 338 || _dot == 340)
                {
                    _bgNextTileID = PPU_Read((ushort)(0x2000 | (_vram.Reg & 0x0FFF)));
                }

                if (_scanline == -1 && _dot >= 280 && _dot < 305)
                {
                    TransferAddressY();
                }

                // Foreground Rendering //
                if (_scanline >= 0 && _dot == 257)
                {
                    Array.Fill<byte>(_scanlineOAM, 0xFF); // Set secondary OAM to default value
                    ClearSpriteShifters();
                    _spriteCount = 0;
                    _oamIndex = 0;
                    _spriteZeroHitPossible = false;
                    // Calculuate sprite height once per scanline
                    byte spriteHeight = (byte)((_ppuCtrl & (byte)PPUCTRL.SPRITE_SIZE) != 0 ? 16 : 8); 

                    while (_oamIndex < 64 && _spriteCount < 9) // Loop until 9 sprites to detect overflow
                    {
                        // Result may be negative, cast to signed short
                        short offset = (short)(_scanline - OAMEntries[_oamIndex].y); 

                        if (offset >= 0 && offset < spriteHeight)
                        {
                            if (_spriteCount < 8)
                            {
                                if (_oamIndex == 0)
                                {
                                    _spriteZeroHitPossible = true;
                                }
                                ScanlineOAM[_spriteCount] = OAMEntries[_oamIndex];
                            }
                            _spriteCount++;
                        }
                        _oamIndex++;
                    }
                    if (_spriteCount > 8)
                    {
                        _ppuStatus |= (byte)PPUSTATUS.SPRITE_OVERFLOW;
                        _spriteCount = 8; // Prevent index out of bounds exceptions when spritecount exceeds 8
                    }
                }

                if (_dot == 340)
                {
                    // I hope this works
                    for (int i = 0; i < _spriteCount; i++)
                    {
                        byte sprPatternBitsLo, sprPatternBitsHi;
                        ushort sprPatternAddrLo, sprPatternAddrHi;

                        bool verticallyFlipped = ((ScanlineOAM[i].attrib & 0x80) != 0);
                        bool topHalf = (_scanline - ScanlineOAM[i].y < 8);

                        if ((_ppuCtrl & (byte)PPUCTRL.SPRITE_SIZE) == 0) { // 8x8 Sprite Mode
                            sprPatternAddrLo = GetPatternAddressLo8x8(verticallyFlipped, i);
                        }
                        else // 8x16 Sprite Mode
                        {
                            sprPatternAddrLo = GetPatternAddressLo8x16(verticallyFlipped, topHalf, i);
                        }

                        sprPatternAddrHi = (ushort)(sprPatternAddrLo + 8); // The Hi address is always exactly 8 bits away
                        sprPatternBitsLo = PPU_Read(sprPatternAddrLo);
                        sprPatternBitsHi = PPU_Read(sprPatternAddrHi);

                        if ((ScanlineOAM[i].attrib & 0x40) != 0) // Sprite is flipped horizontally
                        {
                            sprPatternBitsLo = ReverseByte(sprPatternBitsLo);
                            sprPatternBitsHi = ReverseByte(sprPatternBitsHi);
                        }

                        _spriteShifterPatternLo[i] = sprPatternBitsLo;
                        _spriteShifterPatternHi[i] = sprPatternBitsHi;
                    }
                }
            }

            if (_scanline >= 241 && _scanline < 261)
            {
                if (_scanline == 241 && _dot == 1)
                {
                    _ppuStatus |= (byte)PPUSTATUS.VERTICAL_BLANK;
                    if ((_ppuCtrl & (byte)PPUCTRL.ENABLE_NMI) != 0)
                    {
                        _NMI_Enable = true;
                    }
                }
            }

            // Background Composition //
            byte bg_pixel = 0x00;
            byte bg_palette = 0x00;

            if ((_ppuMask & (byte)PPUMASK.RENDER_BG) != 0)
            {
                ushort bit_mux = (ushort)(0x8000 >> _fineX);

                byte p0_pixel = (byte)((_bgShifterPatternLo & bit_mux) > 0 ? 1 : 0);
                byte p1_pixel = (byte)((_bgShifterPatternHi & bit_mux) > 0 ? 1 : 0);

                bg_pixel = (byte)(((p1_pixel) << 1) | (p0_pixel));

                byte p0_pal = (byte)((_bgShifterAttribLo & bit_mux) > 0 ? 1 : 0);
                byte p1_pal = (byte)((_bgShifterAttribHi & bit_mux) > 0 ? 1 : 0);

                bg_palette = (byte)((p1_pal << 1) | p0_pal);
            }

            // Foreground Composition //
            byte fg_pixel = 0x00;
            byte fg_palette = 0x00;
            byte fg_prio = 0x00;

            if ((_ppuMask & (byte)PPUMASK.RENDER_SPRITES) != 0)
            {
                _spriteZeroRendering = false;
                // Huge credit to OLC here
                for (int i = 0; i < _spriteCount; i++)
                {
                    if (ScanlineOAM[i].x == 0)
                    {
                        // The relevant data to be rendered is at the high bit of the shifters
                        byte fg_pixel_lo = (byte)((_spriteShifterPatternLo[i] & 0x80) > 0 ? 1 : 0);
                        byte fg_pixel_hi = (byte)((_spriteShifterPatternHi[i] & 0x80) > 0 ? 1 : 0);
                        fg_pixel = (byte)((fg_pixel_hi << 1) | fg_pixel_lo);

                        fg_palette = (byte)((ScanlineOAM[i].attrib & 0x03) + 0x04);
                        fg_prio = (byte)((ScanlineOAM[i].attrib & 0x20) == 0  ? 1 : 0);

                        // As soon as we find a non-transparent pixel, we can exit and draw it
                        if (fg_pixel != 0)
                        {
                            if (i == 0)
                            {
                                _spriteZeroRendering = true;
                            }
                            break;
                        }
                    }
                }
            }

            // Background + Foreground Composition //  
            byte pixel = 0x00, palette = 0x00;
            SelectPixelsForRendering(bg_pixel, bg_palette, fg_pixel, fg_palette, fg_prio, out pixel, out palette);

            // Drawing //
            if (_scanline >= 0 && _scanline < 240 && _dot > 0 && _dot <= 256)
            {
                _frameBuffer[_scanline * 256 + (_dot - 1)] = GetColorFromPaletteRAM(palette, pixel);
            }

            _dot++;
            if (_dot >= 341)
            {
                _dot = 0;
                _scanline++;
                if (_scanline >= 261)
                {
                    _scanline = -1;
                    _isFrameComplete = true;
                }
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
            _bgShifterPatternLo = (ushort)((_bgShifterPatternLo & 0xFF00) | _bgNextTileLSB);
            _bgShifterPatternHi = (ushort)((_bgShifterPatternHi & 0xFF00) | _bgNextTileMSB);
            _bgShifterAttribLo = (ushort)((_bgShifterAttribLo & 0xFF00) | ((_bgNextTileAttrib & 0b01) != 0 ? 0xFF : 0x00));
            _bgShifterAttribHi = (ushort)((_bgShifterAttribHi & 0xFF00) | ((_bgNextTileAttrib & 0b10) != 0 ? 0xFF : 0x00));
        }

        public ushort GetPatternAddressLo8x8(bool flippedVertically, int oamIndex)
        {
            ushort sprPatternAddrLo = 0;
            ushort baseAddress = (ushort)(((_ppuCtrl & (byte)PPUCTRL.PATTERN_SPRITE) >> 3) << 12);
            byte row = (byte)(_scanline - ScanlineOAM[oamIndex].y);

            if (!flippedVertically)
            {
                sprPatternAddrLo = (ushort)((baseAddress | (ScanlineOAM[oamIndex].id << 4) | row));
            } 
            else
            {
                sprPatternAddrLo = (ushort)((baseAddress | (ScanlineOAM[oamIndex].id << 4) | (7 - row)));
            }
            return sprPatternAddrLo;
        }

        public ushort GetPatternAddressLo8x16(bool flippedVertically, bool topHalf, int oamIndex)
        {
            ushort sprPatternAddrLo = 0;
            byte verticalOffset = (byte)(topHalf ? 0 : 1);
            byte row = (byte)((_scanline - ScanlineOAM[oamIndex].y) & 0x07);
            byte tileID = ScanlineOAM[oamIndex].id;

            if (!flippedVertically)
            {
                sprPatternAddrLo = (ushort)(((tileID & 1) << 12) | (((tileID & 0xFE) + verticalOffset) << 4) | row);
            } 
            else
            {
                sprPatternAddrLo = (ushort)(((tileID & 1) << 12) | (((tileID & 0xFE) + (1 - verticalOffset)) << 4) | (7 - row));
            }
            return sprPatternAddrLo;
        }

        public void UpdateShifters()
        {
            if ((_ppuMask & (byte)PPUMASK.RENDER_BG) != 0)
            {
                _bgShifterPatternLo <<= 1;
                _bgShifterPatternHi <<= 1;
                _bgShifterAttribLo <<= 1;
                _bgShifterAttribHi <<= 1;
            }

            // Update Sprite Shifters
            if ((_ppuMask & (byte)PPUMASK.RENDER_SPRITES) != 0 && (_dot >= 1 && _dot < 258))
            {
                for (int i = 0; i < _spriteCount; i++)
                {
                    if (ScanlineOAM[i].x > 0)
                    {
                        ScanlineOAM[i].x--;
                    }
                    else
                    {
                        _spriteShifterPatternLo[i] <<= 1;
                        _spriteShifterPatternHi[i] <<= 1;
                    }
                }
            }
        }

        public uint GetColorFromPaletteRAM(byte palette, byte pixel)
        {
            uint color = _nesMasterPalette[PPU_Read((ushort)(0x3F00 + (palette << 2) + pixel)) & 0x3F];
            return color;
        }

        public void SelectPixelsForRendering(byte bg_pixel, byte bg_palette, byte fg_pixel, byte fg_palette, byte fg_prio, out byte pixel, out byte palette)
        {
            pixel = 0x00;
            palette = 0x00;

            // Both transparent, no winner
            if (bg_pixel == 0 && fg_pixel == 0)
            {
                return;
            }
            // Foreground wins
            else if (bg_pixel == 0 && fg_pixel > 0)
            {
                pixel = fg_pixel;
                palette = fg_palette;
            }
            // Background wins
            else if (bg_pixel > 0 && fg_pixel == 0)
            {
                pixel = bg_pixel;
                palette = bg_palette;
            }
            // Tie, priority decides
            else if (bg_pixel > 0 && fg_pixel > 0)
            {
                if (fg_prio > 0)
                {
                    pixel = fg_pixel;
                    palette = fg_palette;
                }
                else
                {
                    pixel = bg_pixel;
                    palette = bg_palette;
                }
                // Since opaque pixels have collided, a sprite zero hit is possible
                if ((_spriteZeroHitPossible && _spriteZeroRendering)
                    && (_ppuMask & (byte)(PPUMASK.RENDER_SPRITES | PPUMASK.RENDER_BG)) == 0b0001_1000) // Both render sprites and render bg is enabled
                {
                    byte startDot;
                    // If left rendering is disabled, skip the leftmost 8 pixels
                    startDot = (byte)(((_ppuMask & (byte)(PPUMASK.RENDER_BG_LEFT | PPUMASK.RENDER_SPRITES_LEFT)) == 0) ? 9 : 1);
                    if (_dot >= startDot && _dot < 258)
                    {
                        _ppuStatus |= (byte)PPUSTATUS.SPRITE_ZERO_HIT;
                    }
                }
            }
        }

        public byte ReverseByte(byte b)
        {
            b = (byte)(((b & 0xF0) >> 4) | ((b & 0x0F) << 4));
            b = (byte)(((b & 0xCC) >> 2) | ((b & 0x33) << 2));
            b = (byte)(((b & 0xAA) >> 1) | ((b & 0x55) << 1));
            return b;
        }

        public void ConnectCartridge(NES_Cartridge cart)
        {
            _cart = cart;
        }
        
        public void ClearSpriteShifters()
        {
            for (byte i = 0; i < 8; i++)
            {
                _spriteShifterPatternLo[i] = 0;
                _spriteShifterPatternHi[i] = 0;
            }
        }

        public void Reset()
        {
            _fineX = 0x00;
            _addressLatch = 0x00;
            _dataBuffer = 0x00;
            _scanline = 0;
            _dot = 0;
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
            _vram.Reg = 0x00;
            _tram.Reg = 0x00;
        }
    }
}
