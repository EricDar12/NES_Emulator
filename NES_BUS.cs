using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    internal class NES_BUS
    {
        public byte[] _memory = new byte[2048]; // 2KB of RAM

        public byte ReadByte(ushort addr)
        {
            if (addr >= 0x0000 && addr < _memory.Length) {
                return _memory[addr];
            }
            Console.WriteLine("Out Of Bounds Read");
            return 0;
        }

        public void WriteByte(ushort addr, byte data)
        {
            if (addr >= 0x0000 && addr < _memory.Length) {
                _memory[addr] = data;
            }
        }

        private void ClearMemory()
        {
            foreach (byte b in _memory) {
                _memory[b] = 0;
            }
        }

    }
}
