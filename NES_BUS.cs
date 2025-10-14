using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    public class NES_BUS
    {
        private byte[] _cpuRAM = new byte[2048]; // 2 KB Memory
        private NES_PPU _ppu;
        private NES_Cartridge? _cart;
        public byte[] _controller = new byte[2];
        public byte[] _controllerState = new byte[2];

        public NES_BUS(NES_PPU ppu)
        {
            _ppu = ppu;
        }

        public byte CPU_Read(ushort addr)
        {
            byte data = 0x00;
        
            if (_cart != null && _cart.CPU_Read(addr, out data))
            {
                // If the cartridge can handle the read, it will set "data" via out variable
            }

            else if (addr >= 0x0000 && addr <= 0x1FFF) {
                data = _cpuRAM[addr & 0x07FF];
            }

            else if (addr >= 0x2000 && addr <= 0x3FFF)
            {
                data = _ppu.CPU_Read((ushort)(addr & 0x0007));
            }

            else if (addr >= 0x4016 && addr <= 0x4017)
            { // Controller addressing range
                data = (_controllerState[addr & 0x0001] & 0x80) > 0 ? (byte)1 : (byte)0;
                _controllerState[addr & 0x0001] <<= 1;
            }

            return data;
        }

        public void CPU_Write(ushort addr, byte data)
        {

            if (_cart != null && _cart.CPU_Write(addr, data))
            {
                
            }

            else if (addr >= 0x0000 && addr <= 0x1FFF) {
                // 2KiB of RAM is mirrored over an 8KiB space
                _cpuRAM[addr & 0x07FF] = data;
            }

            else if (addr >= 0x2000 && addr <= 0x3FFF)
            {
                if ((addr & 0x0007) == 0x0005)
                {
                    Console.WriteLine($"Scroll Write: 0x{data:X2} addr=0x{addr:X4}");
                }
                _ppu.CPU_Write((ushort)(addr & 0x0007), data);
            }

            else if (addr >= 0x4016 && addr <= 0x4017)
            {
                _controllerState[addr & 0x0001] = _controller[addr & 0x0001];
            }
        }

        public void ConnectCartridge(NES_Cartridge cart)
        {
            _cart = cart;
        }

        private void ClearMemory()
        {
            foreach (byte b in _cpuRAM) {
                _cpuRAM[b] = 0;
            }
        }
    }
}
