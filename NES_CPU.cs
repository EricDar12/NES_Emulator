using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    internal class NES_CPU
    {
        internal byte _accumulator;
        internal byte _register_x;
        internal byte _register_y;
        internal ushort _program_counter;
        internal byte _status;
        internal byte _stack_pointer;
        internal UInt32 _master_cycle = 0;
        internal NES_BUS _bus;

        public NES_CPU(NES_BUS bus)
        {
            _bus = bus;
        }

        #region ##### Addressing Modes #####

        public ushort Addr_Immediate()
        {
            return _program_counter++;
        }

        public ushort Addr_ZeroPage()
        {
            return (ushort) _bus.ReadByte(_program_counter++);
        }

        public ushort Addr_ZeroPageX()
        {
            ushort addr = _bus.ReadByte(_program_counter++);
            return (byte) (addr + _register_x); // Byte math ensures rollover within the zero page
        }

        public ushort Addr_ZeroPageY()
        {
            ushort addr = _bus.ReadByte(_program_counter++);
            return (byte) (addr + _register_y);
        }
        
        public ushort Addr_Absolute()
        {
            byte lo = _bus.ReadByte(_program_counter++);
            return (ushort) ((_bus.ReadByte(_program_counter++) << 8) | lo);
        }

        public ushort Addr_AbsoluteX()
        {
            byte lo = _bus.ReadByte(_program_counter++);
            byte hi = _bus.ReadByte(_program_counter++);

            ushort baseAddr = (ushort)((hi << 8 | lo));
            ushort finalAddr = (ushort)((hi << 8 | lo) + _register_x);

            if (IsPageCrossed(baseAddr, finalAddr)) {

            }

            return finalAddr;
        }

        public ushort Addr_AbsoluteY()
        {
            byte lo = _bus.ReadByte(_program_counter++);
            byte hi = _bus.ReadByte(_program_counter++);

            ushort baseAddr = (ushort)((hi << 8 | lo));
            ushort finalAddr = (ushort)((hi << 8 | lo) + _register_y);


            if (IsPageCrossed(baseAddr, finalAddr)) {

            }

            return finalAddr;
        }

        public ushort Addr_Indirect()
        {
            byte ptr_lo = _bus.ReadByte(_program_counter++);
            byte ptr_hi = _bus.ReadByte(_program_counter++);

            ushort ptr = (ushort) ((ptr_hi << 8) | ptr_lo);

            byte lo = _bus.ReadByte(ptr);
            byte hi;

            // 6502 Page Wrapping Bug
            if (ptr_lo == 0xFF) {
                hi = _bus.ReadByte((ushort) (ptr & 0xFF00));
            } else {
                hi = _bus.ReadByte((ushort) (ptr + 1));
            }

            return (ushort) ((hi << 8) | lo);
        }

        public ushort Addr_IndirectX()
        {
            byte b = _bus.ReadByte(_program_counter++);

            ushort lo = _bus.ReadByte((ushort)((b + _register_x) & 0x00FF));
            ushort hi = _bus.ReadByte((ushort)((b + _register_x + 1) & 0x00FF));

            return (ushort) ((hi << 8) | lo);
        }

        public ushort Addr_IndirectY()
        {
            byte b = _bus.ReadByte(_program_counter++);

            ushort lo = _bus.ReadByte((ushort)((b) & 0x00FF));
            ushort hi = _bus.ReadByte((ushort)((b + 1) & 0x00FF));

            ushort baseAddr = (ushort) ((hi << 8) | lo);
            ushort finalAddr = (ushort) (baseAddr + _register_y);

            if (IsPageCrossed(baseAddr, finalAddr)) {

            }

            return finalAddr;
        }

        public bool IsPageCrossed(ushort baseAddr, ushort finalAddr)
        {
            // Page crossing results in one additional clock cycle
            return (baseAddr & 0xFF00) != (finalAddr & 0xFF00);
        }

        #endregion

        [Flags]
        internal enum StatusFlags : byte
        {
            Carry = 1 << 0,
            Zero = 1 << 1,
            InterruptDisable = 1 << 2,
            Decimal = 1 << 3,
            Break = 1 << 4,
            Unused = 1 << 5,
            Overflow = 1 << 6,
            Negative = 1 << 7
        }

        public bool IsFlagSet(StatusFlags flag)
        {
            return (_status & (byte)flag) != 0;
        }

        public void SetFlag(StatusFlags flag, bool value)
        {
            if (value) {
                _status |= (byte)flag;
            }
            else {
                _status &= (byte)~flag;
            }
        }
    }
}
