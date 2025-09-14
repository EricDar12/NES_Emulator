using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    public class NES_Cartridge
    {
        public List<byte> _prgMemory = new List<byte>();
        public List<byte> _chrMemory = new List<byte>();
        public byte _mapperID = 0;
        public byte _prgBanks = 0;
        public byte _chrBanks = 0;
        public Mirror _mirror = Mirror.HORIZONTAL;
        public bool isImageValid = false;

        public NES_Cartridge(string filePath)
        {
            INES_Header header = new INES_Header();

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            byte[] buffer = new byte[16];
            fs.Read(buffer, 0, 16);

            unsafe
            {
                fixed (byte* ptr = buffer)
                {
                    header = Marshal.PtrToStructure<INES_Header>((IntPtr)ptr);
                }
            }

            // If Trainer data is present (512 bytes), skip it
            if ((header.mapper1 & 0x04) != 0)
            {
                fs.Seek(512, SeekOrigin.Current);
            }

            _mapperID = (byte) (((header.mapper2 >> 4) << 4) | (header.mapper1 >> 4));
            _mirror = (header.mapper1 & 0x01) != 0 ? Mirror.VERTICAL : Mirror.HORIZONTAL;

            byte fileFormat = 1;

            if (fileFormat == 0)
            {

            }

            if (fileFormat == 1)
            {

            }

            if (fileFormat == 3)
            {

            }

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct INES_Header
        {
            public fixed byte name[4];
            public byte prg_rom_chunks;
            public byte chr_rom_chunks;
            public byte mapper1;
            public byte mapper2;
            public byte prg_rom_size;
            public byte tv_system1;
            public byte tv_system2;
            public fixed byte unused[5];
        }

        public enum Mirror
        {
            HORIZONTAL,
            VERTICAL,
            ONESCREEN_LO,
            ONESCREEN_HI
        }

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
