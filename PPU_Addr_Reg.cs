using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    public class PPU_Addr_Reg
    {

        public ushort _reg { get; set; } = 0x00;

        public ushort CoarseX
        {
            get => (ushort)(_reg & 0x1F);
            set => _reg = (ushort)(((_reg & ~0x1F) | (value & 0x1F)) & 0x3FFF);
        }        

        public ushort CoarseY
        {
            get => (ushort)((_reg >> 5) & 0x1F);
            set => _reg = (ushort)(((_reg & ~(0x1F << 5)) | ((value & 0x1F) << 5)) & 0x3FFF);
        }

        public ushort NameTableX
        {
            get => (ushort)((_reg >> 10) & 0x01);
            set => _reg = (ushort)(((_reg & ~(0x01 << 10)) | ((value & 0x01) << 10)) & 0x3FFF); 
        }

        public ushort NameTableY
        {
            get => (ushort)((_reg >> 11) & 0x01);
            set => _reg = (ushort)(((_reg & ~(0x01 << 11)) | ((value & 0x01) << 11)) & 0x3FFF); 
        }

        public ushort FineY
        {   
            get => (ushort)((_reg >> 12) & 0x07);
            set => _reg = (ushort)(((_reg & ~(0x07 << 12)) | ((value & 0x07) << 12)) & 0x3FFF);
        }
    }
}
