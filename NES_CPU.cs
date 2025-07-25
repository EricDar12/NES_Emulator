using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
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
            return (ushort)_bus.ReadByte(_program_counter++);
        }

        public ushort Addr_ZeroPageX()
        {
            ushort addr = _bus.ReadByte(_program_counter++);
            return (byte)(addr + _register_x); // Byte math ensures rollover within the zero page
        }

        public ushort Addr_ZeroPageY()
        {
            ushort addr = _bus.ReadByte(_program_counter++);
            return (byte)(addr + _register_y);
        }

        public ushort Addr_Absolute()
        {
            byte lo = _bus.ReadByte(_program_counter++);
            return (ushort)((_bus.ReadByte(_program_counter++) << 8) | lo);
        }

        public ushort Addr_AbsoluteX()
        {
            byte lo = _bus.ReadByte(_program_counter++);
            byte hi = _bus.ReadByte(_program_counter++);

            ushort baseAddr = (ushort)((hi << 8 | lo));
            ushort finalAddr = (ushort)(baseAddr + _register_x);

            IsPageCrossed(baseAddr, finalAddr);

            return finalAddr;
        }

        public ushort Addr_AbsoluteY()
        {
            byte lo = _bus.ReadByte(_program_counter++);
            byte hi = _bus.ReadByte(_program_counter++);

            ushort baseAddr = (ushort)((hi << 8 | lo));
            ushort finalAddr = (ushort)(baseAddr + _register_y);

            IsPageCrossed(baseAddr, finalAddr);

            return finalAddr;
        }

        public ushort Addr_Indirect()
        {
            byte ptr_lo = _bus.ReadByte(_program_counter++);
            byte ptr_hi = _bus.ReadByte(_program_counter++);

            ushort ptr = (ushort)((ptr_hi << 8) | ptr_lo);

            byte lo = _bus.ReadByte(ptr);
            byte hi;

            // 6502 Page Wrapping Bug
            if (ptr_lo == 0xFF) {
                // If we are on the final byte of the page, wrap back to the 0th byte of the same page
                hi = _bus.ReadByte((ushort)(ptr & 0xFF00));
            } else {
                hi = _bus.ReadByte((ushort)(ptr + 1));
            }

            return (ushort)((hi << 8) | lo);
        }

        public ushort Addr_IndirectX()
        {
            byte b = _bus.ReadByte(_program_counter++);

            ushort lo = _bus.ReadByte((ushort)((b + _register_x) & 0x00FF));
            ushort hi = _bus.ReadByte((ushort)((b + _register_x + 1) & 0x00FF));

            return (ushort)((hi << 8) | lo);
        }

        public ushort Addr_IndirectY()
        {
            byte b = _bus.ReadByte(_program_counter++);

            ushort lo = _bus.ReadByte((ushort)((b) & 0x00FF));
            ushort hi = _bus.ReadByte((ushort)((b + 1) & 0x00FF));

            ushort baseAddr = (ushort)((hi << 8) | lo);
            ushort finalAddr = (ushort)(baseAddr + _register_y);

            IsPageCrossed(baseAddr, finalAddr);

            return finalAddr;
        }

        public ushort Addr_Relative()
        {
            sbyte offset = (sbyte)_bus.ReadByte(_program_counter++);
            ushort baseAddr = _program_counter;
            ushort finalAddr = (ushort)((short)_program_counter + offset);

            IsPageCrossed(baseAddr, finalAddr);

            return finalAddr;
        }
        #endregion

        #region ##### Stack Operations #####

        public void PushByte(byte data)
        {
            ushort addr = (ushort) (0x0100 + _stack_pointer);
            _bus.WriteByte(addr, data);
            _stack_pointer--;
        }

        public byte PopByte()
        {
            _stack_pointer++;
            ushort addr = (ushort) (0x0100 + _stack_pointer);
            return _bus.ReadByte(addr);
        }

        public void PushWord(ushort data)
        {
            PushByte((byte)(data >> 8));
            PushByte((byte) data);
        }

        public ushort PopWord() 
        {
            byte lo = PopByte();    
            byte hi = PopByte();
            return (ushort)((hi << 8) | lo);
        }

        #endregion

        #region ##### CPU Instruction #####

        public void LDA(ushort addr, byte cycles)
        {
            _accumulator = _bus.ReadByte(addr);
            SetFlag(StatusFlags.Zero, _accumulator == 0);
            SetFlag(StatusFlags.Negative, (_accumulator & 0x80) != 0);
            _master_cycle += cycles;
        }

        public void STA(ushort addr, byte cycles)
        {
            _bus.WriteByte(addr, _accumulator);
            _master_cycle += cycles;
        }

        public void BRK()
        {
            _program_counter += 2;

            PushWord(_program_counter);

            Console.WriteLine($"DEBUG:Addr Pushed To Stack {_program_counter}"); 

            SetFlag(StatusFlags.Break, true);
            SetFlag(StatusFlags.InterruptDisable, true);
            PushByte((byte) (_status | 0b0011_0000));

            byte lo = _bus.ReadByte(0xFFFE);
            byte hi = _bus.ReadByte(0xFFFF);

            _program_counter = (ushort) ((hi << 8) | lo);
            _master_cycle += 7;
        }

        #region ##### Instruction Variants #####

        void LDA_Immediate() => LDA(Addr_Immediate(), 2); // A9
        void LDA_ZeroPage() => LDA(Addr_ZeroPage(), 3); // A5
        void LDA_ZeroPageX() => LDA(Addr_ZeroPageX(), 4); // B5
        void LDA_Absolute() => LDA(Addr_Absolute(), 4); // AD
        void LDA_AbsoluteX() => LDA(Addr_AbsoluteX(), 4); // BD
        void LDA_AbsoluteY() => LDA(Addr_AbsoluteY(), 4); // B9
        void LDA_IndirectX() => LDA(Addr_IndirectX(), 6); // A1
        void LDA_IndirectY() => LDA(Addr_IndirectY(), 5); // B1

        void STA_ZeroPage() => STA(Addr_ZeroPage(), 3); // 85
        void STA_ZeroPageX() => STA(Addr_ZeroPageX(), 4); // 95
        void STA_Absolute() => STA(Addr_Absolute(), 4); // 8D
        void STA_AbsoluteX() => STA(Addr_AbsoluteX(), 5); // 9D
        void STA_AbsoluteY() => STA(Addr_AbsoluteY(), 5); // 99
        void STA_IndirectX() => STA(Addr_IndirectX(), 6); // 81
        void STA_IndirectY() => STA(Addr_IndirectY(), 6); // 91

        #endregion

        #endregion

        public void Step()
        {
            byte opcode = _bus.ReadByte(_program_counter++);
            switch (opcode)
            {

                // LDA Instructions
                case 0xA9: LDA_Immediate(); break;
                case 0xA5: LDA_ZeroPage(); break;
                case 0xB5: LDA_ZeroPageX(); break;
                case 0xAD: LDA_Absolute(); break;
                case 0xBD: LDA_AbsoluteX(); break;
                case 0xB9: LDA_AbsoluteY(); break;
                case 0xA1: LDA_IndirectX(); break;
                case 0xB1: LDA_IndirectY(); break;

                // STA Instructions
                case 0x85: STA_ZeroPage(); break;
                case 0x95: STA_ZeroPageX(); break;
                case 0x8D: STA_Absolute(); break;
                case 0x9D: STA_AbsoluteX(); break;
                case 0x99: STA_AbsoluteY(); break;
                case 0x81: STA_IndirectX(); break;
                case 0x91: STA_IndirectY(); break;

                default: Console.WriteLine($"No Match For Opcode {opcode}"); break;
            }
        }

        public void FetchAndDecode()
        {
            while (true)
            {
                if (_bus.ReadByte(_program_counter) == 0x00)
                {
                    BRK();
                    Console.WriteLine("Break Reached");
                    break;
                }
                Step();
            }
        }

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

        public void IsPageCrossed(ushort baseAddr, ushort finalAddr)
        {
            if ((baseAddr & 0xFF00) != (finalAddr & 0xFF00))
            {
                _master_cycle += 1;
            }
        }

        public void LoadAndRun(byte[] program)
        {
            LoadProgram(program);
            Reset();
            FetchAndDecode();
        }

        public void Reset()
        {
            // TODO Store these as const data somewhere rather than magic numbers
            _stack_pointer = 0xFD;
            _accumulator = 0x00;
            _register_x = 0x00;
            _status = 0b0010_0000;
            _program_counter = 0xFFFC;
            _master_cycle = 8;

            // Read reset vector at FFFC - FFFD
            byte lo = _bus.ReadByte(0xFFFC);
            byte hi = _bus.ReadByte(0xFFFD);

            _program_counter = (ushort) ((hi << 8) | lo);
        }

        public void LoadProgram(byte[] program, ushort loadAddress = 0x0600)
        {
            for (int i = 0; i < program.Length; i++)
            {
                _bus.WriteByte((ushort)(loadAddress + i), program[i]);
            }

            _bus.WriteByte(0xFFFC, (byte)loadAddress);
            _bus.WriteByte(0xFFFD, (byte)(loadAddress >> 8));
        }
    }
}
