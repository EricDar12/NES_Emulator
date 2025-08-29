using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    public class NES_BUS
    {

        public byte[] _memory = new byte[65536]; // 64 KB Memory

        public byte ReadByte(ushort addr)
        {
            if (addr >= 0x0000 && addr <= _memory.Length) {
                return _memory[addr];
            }
            Console.WriteLine($"Out Of Bounds Read At: {addr}");
            return 0;
        }

        public void WriteByte(ushort addr, byte data)
        {
            if (addr >= 0x0000 && addr <= _memory.Length) {
                _memory[addr] = data;
            }
            else Console.WriteLine($"Out Of Bounds Write At: {addr}");
        }

        private void ClearMemory()
        {
            foreach (byte b in _memory) {
                _memory[b] = 0;
            }
        }

    }
}
