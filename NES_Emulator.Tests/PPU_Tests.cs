using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NES_Emulator.NES_PPU;

namespace NES_Emulator.Tests
{
    public class PPU_Tests
    {

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

    }
}
