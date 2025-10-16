using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    public class NES_Cartridge
    {
        public byte[] _prgMemory;
        public byte[] _chrMemory;
        public byte _mapperID = 0;
        public byte _prgBanks = 0;
        public byte _chrBanks = 0;
        public NES_Mapper _mapper;
        public Mirror _mirror = Mirror.HORIZONTAL;
        public bool _isImageValid = false;

        private const int PROGRAM_MEMORY_UNIT = 16384;
        private const int CHARACTER_MEMORY_UNIT = 8192;

        public NES_Cartridge()
        {

        }

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

            _mapperID = (byte)(((header.mapper2 >> 4) << 4) | (header.mapper1 >> 4));
            _mirror = (header.mapper1 & 0x01) != 0 ? Mirror.VERTICAL : Mirror.HORIZONTAL;

            // Hardcoded for prototyping
            byte fileFormat = 1;

            if (fileFormat == 0)
            {

            }

            if (fileFormat == 1)
            {
                _prgBanks = header.prg_rom_chunks;
                _prgMemory = new byte[_prgBanks * PROGRAM_MEMORY_UNIT];
                fs.Read(_prgMemory, 0, _prgMemory.Length);

                _chrBanks = header.chr_rom_chunks;
                if (_chrBanks == 0)
                {
                    _chrMemory = new byte[CHARACTER_MEMORY_UNIT];
                } 
                else
                {
                    _chrMemory = new byte[_chrBanks * CHARACTER_MEMORY_UNIT];
                }

                fs.Read(_chrMemory, 0, _chrMemory.Length);
            }

            if (fileFormat == 3)
            {

            }

            // More mappers will come
            switch (_mapperID)
            {
                case 0:
                    _mapper = new Mapper_0(_prgBanks, _chrBanks); break;
                default:
                    _mapper = new Mapper_0(_prgBanks, _chrBanks); break;
            }

            _isImageValid = true;

            Console.WriteLine(
                $"PRGMemory: {_prgMemory?.Length} bytes | " +
                $"CHRMemory: {_chrMemory?.Length} bytes | " +
                $"MapperID: {_mapperID} | " +
                $"PRGBanks: {_prgBanks} | " +
                $"CHRBanks: {_chrBanks} | " +
                $"Mapper: {_mapper.ToString() ?? "null"} | " +
                $"Mirror: {_mirror} | " +
                $"IsImageValid: {_isImageValid}"
            );

            //foreach (byte b in _chrMemory)
            //{
            //    Console.WriteLine($"{b:x4}");
            //}

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

        public bool CPU_Read(ushort addr, out byte data)
        {
            ushort mappedAddr = 0;
            data = 0;

            if (_mapper.CPU_Map_Read(addr, out mappedAddr))
            {
                data = _prgMemory[mappedAddr];
                return true;
            }

            return false;
        }

        public bool CPU_Write(ushort addr, byte data)
        {
            ushort mappedAddr = 0;

            if (_mapper.CPU_Map_Write(addr, out mappedAddr))
            {
                _prgMemory[mappedAddr] = data;
                return true;
            }

            return false;
        }

        public bool PPU_Read(ushort addr, out byte data)
        {
            ushort mappedAddr = 0;
            data = 0;

            if (_mapper.PPU_Map_Read(addr, out mappedAddr))
            {
                //Console.WriteLine($"Mapped Address: {mappedAddr:X4}");
                data = _chrMemory[mappedAddr];
                return true;
            }

            return false;
        }

        public bool PPU_Write(ushort addr, byte data)
        {

            ushort mappedAddr = 0;

            if (_mapper.PPU_Map_Write(addr, out mappedAddr))
            {
                _chrMemory[mappedAddr] = data;
                //Console.WriteLine("Write " + _chrMemory[mappedAddr]);
                return true;
            }

            return false;
        }
    }
}
