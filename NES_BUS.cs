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

        public NES_BUS(NES_PPU ppu)
        {
            _ppu = ppu;
        }

        public byte CPU_Read(ushort addr)
        {
            byte data = 0x00;
        
            if (_cart != null && _cart.CPU_Read(addr))
            {

            }

            else if (addr >= 0x0000 && addr <= 0x1FFF) {
                data = _cpuRAM[addr & 0x07FF];
            }

            else if (addr >= 0x2000 && addr <= 0x3FFF)
            {
                data = _ppu.CPU_Read((ushort)(addr & 0x0007));
            }

            return data;
        }

        public void CPU_Write(ushort addr, byte data)
        {

            if (_cart != null && _cart.CPU_Write(addr, data))
            {

            }

            else if (addr >= 0x0000 && addr <= 0x1FFF) {
                _cpuRAM[addr & 0x07FF] = data;
            }

            else if (addr >= 0x2000 && addr <= 0x3FFF)
            {
                _ppu.CPU_Write((ushort)(addr & 0x0007), data);
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
