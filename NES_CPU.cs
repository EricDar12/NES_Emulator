using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    public class NES_CPU
    {
        public byte _accumulator;
        public byte _register_x;
        public byte _register_y;
        public ushort _program_counter;
        public byte _status = 0b0010_0000;
        public byte _stack_pointer;
        public UInt32 _master_cycle = 0;
        public NES_BUS _bus;

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

            sbyte offset = (sbyte) _bus.ReadByte(_program_counter++);
            ushort baseAddr = _program_counter;
            ushort finalAddr = (ushort) (baseAddr + offset);

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
            SetNegativeAndZeroFlags(_accumulator);
            _master_cycle += cycles;
        }

        public void STA(ushort addr, byte cycles)
        {
            _bus.WriteByte(addr, _accumulator);
            _master_cycle += cycles;
        }

        public void LDX(ushort addr, byte cycles)
        {
            _register_x = _bus.ReadByte(addr);
            SetNegativeAndZeroFlags(_register_x);
            _master_cycle += cycles;
        }

        public void STX(ushort addr, byte cycles)
        {
            _bus.WriteByte(addr, _register_x);
            _master_cycle += cycles;
        }

        public void LDY(ushort addr, byte cycles)
        {
            _register_y = _bus.ReadByte(addr);
            SetNegativeAndZeroFlags(_register_y);
            _master_cycle += cycles;
        }

        public void STY(ushort addr, byte cycles)
        {
            _bus.WriteByte(addr, _register_y);
            _master_cycle += cycles;
        }

        public void TAX(byte cycles = 2)
        {
            _register_x = _accumulator;
            SetNegativeAndZeroFlags(_register_x);
            _master_cycle += cycles;
        }

        public void TXA(byte cycles = 2)
        {
            _accumulator = _register_x;
            SetNegativeAndZeroFlags(_accumulator);
            _master_cycle += cycles;
        }

        public void TAY(byte cycles = 2)
        {
            _register_y = _accumulator;
            SetNegativeAndZeroFlags(_register_y);
            _master_cycle += cycles;
        }

        public void TYA(byte cycles = 2)
        {
            _accumulator = _register_y;
            SetNegativeAndZeroFlags(_accumulator);
            _master_cycle += cycles;
        }

        public void INX(byte cycles = 2)
        {
            _register_x += 1;
            SetNegativeAndZeroFlags(_register_x);
            _master_cycle += cycles;
        }

        public void INY(byte cycles = 2)
        {
            _register_y += 1;
            SetNegativeAndZeroFlags(_register_y);
            _master_cycle += cycles;
        }   

        public void DEX(byte cycles = 2)
        {
            _register_x -= 1;
            SetNegativeAndZeroFlags(_register_x);
            _master_cycle += cycles;
        }

        public void DEY(byte cycles = 2)
        {
            _register_y -= 1;
            SetNegativeAndZeroFlags(_register_y);
            _master_cycle += cycles;
        }

        public void CLC(byte cycles = 2)
        {
            SetFlag(StatusFlags.Carry, false);
            _master_cycle += cycles;
        }

        public void SEC(byte cycles = 2)
        {
            SetFlag(StatusFlags.Carry, true);
            _master_cycle += cycles;
        }

        public void JMP(ushort addr, byte cycles)
        {
            _program_counter = addr;
            _master_cycle += cycles;
        }

        public void JSR(ushort addr,  byte cycles)
        {
            PushWord((ushort)(_program_counter - 1));
            _program_counter = addr;
            _master_cycle += cycles;
        }

        public void BRK()
        {

            PushWord((ushort)(_program_counter + 2));

            Console.WriteLine($"DEBUG:Addr Pushed To Stack {_program_counter}");

            PushByte((byte) (_status | 0b0011_0000));
            SetFlag(StatusFlags.InterruptDisable, true);

            // Read from Interrupt Vector @ FFFE-FFFF
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

        void LDX_Immediate() => LDX(Addr_Immediate(), 2); // A2
        void LDX_ZeroPage() => LDX(Addr_ZeroPage(), 3); // A6
        void LDX_ZeroPageY() => LDX(Addr_ZeroPageY(), 4); // B6
        void LDX_Absolute() => LDX(Addr_Absolute(), 4); // AE
        void LDX_AbsoluteY() => LDX(Addr_AbsoluteY(), 4); // BE

        void STX_ZeroPage() => STX(Addr_ZeroPage(), 3); // 86
        void STX_ZeroPageY() => STX(Addr_ZeroPageY(), 4); // 96
        void STX_Absolute() => STX(Addr_Absolute(), 4); // 8E

        void LDY_Immediate() => LDY(Addr_Immediate(), 2); // A0
        void LDY_ZeroPage() => LDY(Addr_ZeroPage(), 3); // A4
        void LDY_ZeroPageX() => LDY(Addr_ZeroPageX(), 4); // B4
        void LDY_Absolute() => LDY(Addr_Absolute(), 4); // AC
        void LDY_AbsoluteX() => LDY(Addr_AbsoluteX(), 4); // BC

        void STY_ZeroPage() => STY(Addr_ZeroPage(), 3); // 84
        void STY_ZeroPageX() => STY(Addr_ZeroPageX(), 4); // 94
        void STY_Absolute() => STY(Addr_Absolute(), 4); // 8C

        void JMP_Absolute() => JMP(Addr_Absolute(), 3); // 4C
        void JMP_Indirect() => JMP(Addr_Indirect(), 5); // 6C
        void JSR_Absolute() => JSR(Addr_Absolute(), 6); // 20

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

                // LDX Instructions
                case 0xA2: LDX_Immediate(); break;
                case 0xA6: LDX_ZeroPage(); break;
                case 0xB6: LDX_ZeroPageY(); break;
                case 0xAE: LDX_Absolute(); break;
                case 0xBE: LDX_AbsoluteY(); break;

                // STA Instructions
                case 0x85: STA_ZeroPage(); break;
                case 0x95: STA_ZeroPageX(); break;
                case 0x8D: STA_Absolute(); break;
                case 0x9D: STA_AbsoluteX(); break;
                case 0x99: STA_AbsoluteY(); break;
                case 0x81: STA_IndirectX(); break;
                case 0x91: STA_IndirectY(); break;

                // STX Instructions
                case 0x86: STX_ZeroPage(); break;
                case 0x96: STX_ZeroPageY(); break;
                case 0x8E: STX_Absolute(); break;

                // LDY Instructions
                case 0xA0: LDY_Immediate(); break;
                case 0xA4: LDY_ZeroPage(); break;
                case 0xB4: LDY_ZeroPageX(); break;
                case 0xAC: LDY_Absolute(); break;
                case 0xBC: LDY_AbsoluteX(); break;

                // STY Instructions
                case 0x84: STY_ZeroPage(); break;
                case 0x94: STY_ZeroPageX(); break;
                case 0x8C: STY_Absolute(); break;

                // Transfer Instructions
                case 0xAA: TAX(); break;
                case 0x8A: TXA(); break;
                case 0xA8: TAY(); break;
                case 0x98: TYA(); break;

                // Arithmetic Operations
                case 0xE8: INX(); break;
                case 0xCA: DEX(); break;
                case 0xC8: INY(); break;
                case 0x88: DEY(); break;

                // Flag Operations
                case 0x18: CLC(); break;
                case 0x38: SEC(); break;

                // Program Control & Branching
                case 0x4C: JMP_Absolute(); break;
                case 0x6C: JMP_Indirect(); break;
                case 0x20: JSR_Absolute(); break;

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
        public enum StatusFlags : byte
        {
            Carry = 1 << 0,
            Zero = 1 << 1,
            InterruptDisable = 1 << 2,
            Decimal = 1 << 3,
            Break = 1 << 4,
            Unused = 1 << 5, // Always set to 1
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

        public void SetNegativeAndZeroFlags(byte register)
        {
            SetFlag(StatusFlags.Zero, register == 0);
            SetFlag(StatusFlags.Negative, (register & 0x80) != 0);
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
            _register_y = 0x00;
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
