using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using static NES_Emulator.NES_PPU;

namespace NES_Emulator.Tests
{
    public class PPU_Tests
    {

        private readonly ITestOutputHelper _output;

        public PPU_Tests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestPPU_Status()
        {

            NES_System nes = new NES_System();
            Assert.Equal(0, nes._ppu._ppuStatus);

            nes._ppu.SetFlag<NES_PPU.PPUSTATUS>(NES_PPU.PPUSTATUS.VERTICAL_BLANK, ref nes._ppu._ppuStatus, true);
            nes._ppu.SetFlag<NES_PPU.PPUSTATUS>(NES_PPU.PPUSTATUS.SPRITE_ZERO_HIT, ref nes._ppu._ppuStatus, true);
            nes._ppu.SetFlag<NES_PPU.PPUSTATUS>(NES_PPU.PPUSTATUS.SPRITE_OVERFLOW, ref nes._ppu._ppuStatus, true);

            Assert.True(nes._ppu.IsSet<NES_PPU.PPUSTATUS>(NES_PPU.PPUSTATUS.VERTICAL_BLANK, nes._ppu._ppuStatus));
            Assert.True(nes._ppu.IsSet<NES_PPU.PPUSTATUS>(NES_PPU.PPUSTATUS.SPRITE_ZERO_HIT, nes._ppu._ppuStatus));
            Assert.True(nes._ppu.IsSet<NES_PPU.PPUSTATUS>(NES_PPU.PPUSTATUS.SPRITE_OVERFLOW, nes._ppu._ppuStatus));

            nes._ppu.SetFlag<NES_PPU.PPUSTATUS>(NES_PPU.PPUSTATUS.VERTICAL_BLANK, ref nes._ppu._ppuStatus, false);
            nes._ppu.SetFlag<NES_PPU.PPUSTATUS>(NES_PPU.PPUSTATUS.SPRITE_ZERO_HIT, ref nes._ppu._ppuStatus, false);
            nes._ppu.SetFlag<NES_PPU.PPUSTATUS>(NES_PPU.PPUSTATUS.SPRITE_OVERFLOW, ref nes._ppu._ppuStatus, false);

            Assert.False(nes._ppu.IsSet<NES_PPU.PPUSTATUS>(NES_PPU.PPUSTATUS.VERTICAL_BLANK, nes._ppu._ppuStatus));
            Assert.False(nes._ppu.IsSet<NES_PPU.PPUSTATUS>(NES_PPU.PPUSTATUS.SPRITE_ZERO_HIT, nes._ppu._ppuStatus));
            Assert.False(nes._ppu.IsSet<NES_PPU.PPUSTATUS>(NES_PPU.PPUSTATUS.SPRITE_OVERFLOW, nes._ppu._ppuStatus));

            Assert.Equal(0, nes._ppu._ppuStatus);
        }

        [Fact]
        public void TestPPU_Mask()
        {
            NES_System nes = new NES_System();

            Assert.Equal(0, nes._ppu._ppuMask);

            nes._ppu.SetFlag<PPUMASK>(PPUMASK.GRAYSCALE, ref nes._ppu._ppuMask, true);
            nes._ppu.SetFlag<PPUMASK>(PPUMASK.RENDER_BG_LEFT, ref nes._ppu._ppuMask, true);
            nes._ppu.SetFlag<PPUMASK>(PPUMASK.RENDER_SPRITES, ref nes._ppu._ppuMask, true);

            Assert.True(nes._ppu.IsSet<PPUMASK>(PPUMASK.GRAYSCALE, nes._ppu._ppuMask));
            Assert.True(nes._ppu.IsSet<PPUMASK>(PPUMASK.RENDER_BG_LEFT, nes._ppu._ppuMask));
            Assert.True(nes._ppu.IsSet<PPUMASK>(PPUMASK.RENDER_SPRITES, nes._ppu._ppuMask));

            nes._ppu.SetFlag<PPUMASK>(PPUMASK.GRAYSCALE, ref nes._ppu._ppuMask, false);
            nes._ppu.SetFlag<PPUMASK>(PPUMASK.RENDER_BG_LEFT, ref nes._ppu._ppuMask, false);
            nes._ppu.SetFlag<PPUMASK>(PPUMASK.RENDER_SPRITES, ref nes._ppu._ppuMask, false);

            Assert.False(nes._ppu.IsSet<PPUMASK>(PPUMASK.GRAYSCALE, nes._ppu._ppuMask));
            Assert.False(nes._ppu.IsSet<PPUMASK>(PPUMASK.RENDER_BG_LEFT, nes._ppu._ppuMask));
            Assert.False(nes._ppu.IsSet<PPUMASK>(PPUMASK.RENDER_SPRITES, nes._ppu._ppuMask));

            Assert.Equal(0, nes._ppu._ppuMask);
        }

        [Fact]
        public void TestPPU_Control()
        {
            NES_System nes = new NES_System();

            Assert.Equal(0, nes._ppu._ppuCtrl);

            nes._ppu.SetFlag<PPUCTRL>(PPUCTRL.NAMETABLE_X, ref nes._ppu._ppuCtrl, true);
            nes._ppu.SetFlag<PPUCTRL>(PPUCTRL.INCREMENT_MODE, ref nes._ppu._ppuCtrl, true);
            nes._ppu.SetFlag<PPUCTRL>(PPUCTRL.ENABLE_NMI, ref nes._ppu._ppuCtrl, true);

            Assert.True(nes._ppu.IsSet<PPUCTRL>(PPUCTRL.NAMETABLE_X, nes._ppu._ppuCtrl));
            Assert.True(nes._ppu.IsSet<PPUCTRL>(PPUCTRL.INCREMENT_MODE, nes._ppu._ppuCtrl));
            Assert.True(nes._ppu.IsSet<PPUCTRL>(PPUCTRL.ENABLE_NMI, nes._ppu._ppuCtrl));

            nes._ppu.SetFlag<PPUCTRL>(PPUCTRL.NAMETABLE_X, ref nes._ppu._ppuCtrl, false);
            nes._ppu.SetFlag<PPUCTRL>(PPUCTRL.INCREMENT_MODE, ref nes._ppu._ppuCtrl, false);
            nes._ppu.SetFlag<PPUCTRL>(PPUCTRL.ENABLE_NMI, ref nes._ppu._ppuCtrl, false);

            Assert.False(nes._ppu.IsSet<PPUCTRL>(PPUCTRL.NAMETABLE_X, nes._ppu._ppuCtrl));
            Assert.False(nes._ppu.IsSet<PPUCTRL>(PPUCTRL.INCREMENT_MODE, nes._ppu._ppuCtrl));
            Assert.False(nes._ppu.IsSet<PPUCTRL>(PPUCTRL.ENABLE_NMI, nes._ppu._ppuCtrl));

            Assert.Equal(0, nes._ppu._ppuCtrl);
        }

        [Fact]
        public void TestLoopyRegister()
        {
            NES_PPU ppu = new NES_PPU();

            Assert.Equal(0, ppu._tram.Reg);
            Assert.Equal(0, ppu._vram.Reg);

            // Attempt to overflow all the bit ranges, tests masking
            ppu._tram.CoarseX = 0xFFFF;
            ppu._tram.CoarseY = 0xFFFF;
            ppu._tram.NameTableX = 0xFFFF;
            ppu._tram.NameTableY = 0xFFFF;
            ppu._tram.FineY = 0xFFFF;

            ppu._vram.CoarseX = 0xFFFF;
            ppu._vram.CoarseY = 0xFFFF;
            ppu._vram.NameTableX = 0xFFFF;
            ppu._vram.NameTableY = 0xFFFF;
            ppu._vram.FineY = 0xFFFF;

            Assert.Equal(0x7FFF, ppu._tram.Reg);
            Assert.Equal(0x7FFF, ppu._vram.Reg);

            ppu._tram.CoarseX = 0;
            ppu._tram.CoarseY = 0;
            ppu._tram.NameTableX = 0;
            ppu._tram.NameTableY = 0;
            ppu._tram.FineY = 0;

            ppu._vram.CoarseX = 0;
            ppu._vram.CoarseY = 0;
            ppu._vram.NameTableX = 0;
            ppu._vram.NameTableY = 0;
            ppu._vram.FineY = 0;

            Assert.Equal(0, ppu._tram.Reg);
            Assert.Equal(0, ppu._vram.Reg);

            ppu._tram.CoarseX = 0x0003;
            ppu._tram.CoarseY = 0x0007;

            ppu._vram.CoarseX = 0x0009;
            ppu._vram.CoarseY = 0x0011;

            ppu._vram.Reg = ppu._tram.Reg;

            Assert.Equal(ppu._tram.Reg, ppu._vram.Reg);
        }

        [Fact]
        public void TestLoopyAddress()
        {
            NES_PPU ppu = new NES_PPU();
            Assert.Equal(0, ppu._tram.Reg);
            Assert.Equal(0, ppu._vram.Reg);

            byte data = 0x24;

            ppu._tram.Reg = (ushort)((ppu._tram.Reg & 0x00FF) | ((data & 0x3F) << 8)); // Mask data to 6 bits, shift to high byte
            Assert.Equal(0b0010010000000000, ppu._tram.Reg);

            data = 0x00;

            ppu._tram.Reg = (ushort)((ppu._tram.Reg & 0xFF00) | data); // Low 8 bits
            Assert.Equal(0b0010010000000000, ppu._tram.Reg);

            ppu._vram.Reg = ppu._tram.Reg;
            Assert.Equal(ppu._vram.Reg, ppu._tram.Reg);

            data = 0x20;

            ppu._tram.Reg = (ushort)((ppu._tram.Reg & 0x00FF) | ((data & 0x3F) << 8)); // Mask data to 6 bits, shift to high byte
            Assert.Equal(0b0010000000000000, ppu._tram.Reg);

            data = 0x00;

            ppu._tram.Reg = (ushort)((ppu._tram.Reg & 0xFF00) | data); // Low 8 bits
            Assert.Equal(0b0010000000000000, ppu._tram.Reg);

            ppu._vram.Reg = ppu._tram.Reg;
            Assert.Equal(ppu._vram.Reg, ppu._tram.Reg);

            data = 0x23;

            ppu._tram.Reg = (ushort)((ppu._tram.Reg & 0x00FF) | ((data & 0x3F) << 8)); // Mask data to 6 bits, shift to high byte
            Assert.Equal(0b0010001100000000, ppu._tram.Reg);

            data = 0xC2;

            ppu._tram.Reg = (ushort)((ppu._tram.Reg & 0xFF00) | data); // Low 8 bits
            Assert.Equal(0b0010001111000010, ppu._tram.Reg);

            ppu._vram.Reg = ppu._tram.Reg;
            Assert.Equal(ppu._vram.Reg, ppu._tram.Reg);
        }

        [Fact]
        public void TestLoopyRegistersIncrementAndTransfer()
        {
            NES_PPU ppu = new NES_PPU();

            // Enable rendering so increments actually work
            ppu._ppuMask = (byte)(PPUMASK.RENDER_BG | PPUMASK.RENDER_SPRITES);

            // Initialize VRAM and TRAM to zero
            ppu._vram.Reg = 0x0000;
            ppu._tram.Reg = 0x0000;

            Console.WriteLine("=== Initial State ===");
            Console.WriteLine($"VRAM Reg: {ppu._vram.Reg:x4}");
            Console.WriteLine($"TRAM Reg: {ppu._tram.Reg:x4}");
            Console.WriteLine($"FineY: {ppu._vram.FineY}, CoarseX: {ppu._vram.CoarseX}, CoarseY: {ppu._vram.CoarseY}, NTX: {ppu._vram.NameTableX}, NTY: {ppu._vram.NameTableY}");

            // Increment FineY 8 times to force a wrap and increment CoarseY
            for (int i = 0; i < 8; i++)
            {
                ppu.IncrementScrollY();
                Console.WriteLine($"After IncrementScrollY {i + 1}: FineY={ppu._vram.FineY}, CoarseY={ppu._vram.CoarseY}, NTY={ppu._vram.NameTableY}, Reg={ppu._vram.Reg:x4}");
            }

            // CoarseX increment from 30 to 0 should toggle NameTableX
            ppu._vram.CoarseX = 30;
            ppu._vram.NameTableX = 0;
            ppu.IncrementScrollX();
            Console.WriteLine($"After IncrementScrollX from 30: CoarseX={ppu._vram.CoarseX}, NTX={ppu._vram.NameTableX}");

            ppu.IncrementScrollX();
            Console.WriteLine($"After IncrementScrollX wrap: CoarseX={ppu._vram.CoarseX}, NTX={ppu._vram.NameTableX}");

            // Test TransferAddressX
            ppu._tram.Reg = 0b01010101010101;
            ppu._vram.Reg = 0;
            ppu.TransferAddressX();
            Console.WriteLine($"After TransferAddressX: VRAM Reg={ppu._vram.Reg:x4}, CoarseX={ppu._vram.CoarseX}, NTX={ppu._vram.NameTableX}");

            // Test TransferAddressY
            ppu._tram.Reg = 0b10101010101010;
            ppu._vram.Reg = 0;
            ppu.TransferAddressY();
            Console.WriteLine($"After TransferAddressY: VRAM Reg={ppu._vram.Reg:x4}, FineY={ppu._vram.FineY}, CoarseY={ppu._vram.CoarseY}, NTY={ppu._vram.NameTableY}");

            // Assertions to catch obvious mistakes
            Assert.InRange(ppu._vram.FineY, 0, 7);
            Assert.InRange(ppu._vram.CoarseX, 0, 31);
            Assert.InRange(ppu._vram.CoarseY, 0, 29);
            Assert.True(ppu._vram.NameTableX == 0 || ppu._vram.NameTableX == 1);
            Assert.True(ppu._vram.NameTableY == 0 || ppu._vram.NameTableY == 1);
        }
    }
}
