using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            if (ptr_lo == 0xFF)
            {
                // If we are on the final byte of the page, wrap back to the 0th byte of the same page
                hi = _bus.ReadByte((ushort)(ptr & 0xFF00));
            }
            else
            {
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
            ushort finalAddr = (ushort)(baseAddr + offset);
            return finalAddr;
        }
        #endregion
        #region ##### Stack Operations #####

        public void PushByte(byte data)
        {
            ushort addr = (ushort)(0x0100 + _stack_pointer);
            _bus.WriteByte(addr, data);
            _stack_pointer--;
        }

        public byte PopByte()
        {
            _stack_pointer++;
            ushort addr = (ushort)(0x0100 + _stack_pointer);
            return _bus.ReadByte(addr);
        }

        public void PushWord(ushort data)
        {
            PushByte((byte)(data >> 8));
            PushByte((byte)data);
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

        public void TSX(byte cycles = 2)
        {
            _register_x = _stack_pointer;
            SetNegativeAndZeroFlags( _register_x);
            _master_cycle += cycles;
        }

        public void TXS(byte cycles = 2)
        {
            _stack_pointer = _register_x;
            _master_cycle += cycles;
        }

        public void INC(ushort addr, byte cycles)
        {
            byte operand = _bus.ReadByte(addr);
            byte result = (byte) (operand + 1);
            _bus.WriteByte(addr, result);
            SetNegativeAndZeroFlags(result);
            _master_cycle += cycles;
        }

        public void DEC(ushort addr, byte cycles)
        {
            byte operand = _bus.ReadByte(addr);
            byte result = (byte)(operand - 1);
            _bus.WriteByte(addr, result);
            SetNegativeAndZeroFlags(result);
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

        public void CLI(byte cycles = 2)
        {
            SetFlag(StatusFlags.InterruptDisable, false);
            _master_cycle += cycles;
        }

        public void SEI(byte cycles = 2)
        {
            SetFlag(StatusFlags.InterruptDisable, true);
            _master_cycle += cycles;
        }

        public void CLV(byte cycles = 2)
        {
            SetFlag(StatusFlags.Overflow, false);
            _master_cycle += cycles;
        }

        public void CLD(byte cycles = 2)
        {
            SetFlag(StatusFlags.Overflow, false);
            _master_cycle += cycles;
        }

        public void SED(byte cycles = 2)
        {
            SetFlag(StatusFlags.Decimal, true);
            _master_cycle += cycles;
        }

        public void JMP(ushort addr, byte cycles)
        {
            _program_counter = addr;
            _master_cycle += cycles;
        }

        public void JSR(ushort addr, byte cycles)
        {
            PushWord((ushort)(_program_counter - 1));
            _program_counter = addr;
            _master_cycle += cycles;
        }

        public void RTS(byte cycles = 6)
        {
            ushort addr = PopWord();
            _program_counter = (ushort)(addr + 1);
            _master_cycle += cycles;
        }

        public void BEQ(ushort addr, byte cycles)
        {
            if (!IsFlagSet(StatusFlags.Zero))
            {
                _master_cycle += cycles;
                return;
            }
            IsPageCrossed(_program_counter, addr);
            _program_counter = addr;
            _master_cycle += (ushort)(cycles + 1);
        }

        public void BNE(ushort addr, byte cycles)
        {
            if (IsFlagSet(StatusFlags.Zero))
            {
                _master_cycle += cycles;
                return;
            }
            IsPageCrossed(_program_counter, addr);
            _program_counter = addr;
            _master_cycle += (ushort)(cycles + 1);
        }

        public void BCC(ushort addr, byte cycles)
        {
            if (IsFlagSet(StatusFlags.Carry))
            {
                _master_cycle += cycles;
                return;
            }
            IsPageCrossed(_program_counter, addr);
            _program_counter = addr;
            _master_cycle += (ushort)(cycles + 1);
        }

        public void BCS(ushort addr, byte cycles)
        {
            if (!IsFlagSet(StatusFlags.Carry))
            {
                _master_cycle += cycles;
                return;
            }
            IsPageCrossed(_program_counter, addr);
            _program_counter = addr;
            _master_cycle += (ushort)(cycles + 1);
        }

        public void BPL(ushort addr, byte cycles)
        {
            if (IsFlagSet(StatusFlags.Negative))
            {
                _master_cycle += cycles;
                return;
            }
            IsPageCrossed(_program_counter, addr);
            _program_counter = addr;
            _master_cycle += (ushort)(cycles + 1);
        }

        public void BMI(ushort addr, byte cycles)
        {
            if (!IsFlagSet(StatusFlags.Negative))
            {
                _master_cycle += cycles;
                return;
            }
            IsPageCrossed(_program_counter, addr);
            _program_counter = addr;
            _master_cycle += (ushort)(cycles + 1);
        }

        public void BVC(ushort addr, byte cycles)
        {
            if (IsFlagSet(StatusFlags.Overflow))
            {
                _master_cycle += cycles;
                return;
            }
            IsPageCrossed(_program_counter, addr);
            _program_counter = addr;
            _master_cycle += (ushort)(cycles + 1);
        }

        public void BVS(ushort addr, byte cycles)
        {
            if (!IsFlagSet(StatusFlags.Overflow))
            {
                _master_cycle += cycles;
                return;
            }
            IsPageCrossed(_program_counter, addr);
            _program_counter = addr;
            _master_cycle += (ushort)(cycles + 1);
        }

        public void CMP(ushort addr, byte cycles)
        {
            byte operand = _bus.ReadByte(addr);
            byte difference = (byte)(_accumulator - operand);
            SetFlag(StatusFlags.Carry, (_accumulator >= operand));
            SetNegativeAndZeroFlags(difference);
            _master_cycle += cycles;
        }

        public void CPX(ushort addr, byte cycles)
        {
            byte operand = _bus.ReadByte(addr);
            byte difference = (byte)(_register_x - operand);
            SetFlag(StatusFlags.Carry, (_register_x >= operand));
            SetNegativeAndZeroFlags(difference);
            _master_cycle += cycles;
        }

        public void CPY(ushort addr, byte cycles)
        {
            byte operand = _bus.ReadByte(addr);
            byte difference = (byte)(_register_y - operand);
            SetFlag(StatusFlags.Carry, (_register_y >= operand));
            SetNegativeAndZeroFlags(difference);
            _master_cycle += cycles;
        }

        public void ADC(ushort addr, byte cycles)
        {
            byte operand = _bus.ReadByte(addr);
            ushort result = (ushort)(_accumulator + operand + (IsFlagSet(StatusFlags.Carry) ? 1 : 0));
            SetNegativeAndZeroFlags((byte)result);
            SetOverflowAndCarryFlags(result, operand);
            _accumulator = (byte)result;
            _master_cycle += cycles;
        }

        public void SBC(ushort addr, byte cycles)
        {
            byte operand = _bus.ReadByte(addr);
            byte negatedOperand = (byte)~operand;
            ushort result = (ushort)(_accumulator + negatedOperand + (IsFlagSet(StatusFlags.Carry) ? 1 : 0));
            SetNegativeAndZeroFlags((byte)result);
            SetOverflowAndCarryFlags(result, negatedOperand);
            _accumulator = (byte)result;
            _master_cycle += cycles;
        }

        public void AND(ushort addr, byte cycles)
        {
            byte operand = _bus.ReadByte(addr);
            _accumulator &= operand;
            SetNegativeAndZeroFlags(_accumulator);
            _master_cycle += cycles;
        }

        public void ORA(ushort addr, byte cycles)
        {
            byte operand = _bus.ReadByte(addr);
            _accumulator |= operand;
            SetNegativeAndZeroFlags(_accumulator);
            _master_cycle += cycles;
        }

        public void EOR(ushort addr, byte cycles)
        {
            byte operand = _bus.ReadByte(addr);
            _accumulator ^= operand;
            SetNegativeAndZeroFlags(_accumulator);
            _master_cycle += cycles;
        }

        public void ASL(ushort addr, byte cycles, bool implied)
        {

            byte operand = implied ? _accumulator : _bus.ReadByte(addr);
            byte result = (byte)(operand << 1);

            if (implied)
            {
                _accumulator = result;
            }
            else
            {
                _bus.WriteByte(addr, result);
            }
            SetFlag(StatusFlags.Carry, ((operand & 0x80) != 0));
            SetNegativeAndZeroFlags(result);
            _master_cycle += cycles;
        }

        public void LSR(ushort addr, byte cycles, bool implied)
        {
            byte operand = implied ? _accumulator : _bus.ReadByte(addr);
            byte result = (byte)(operand >> 1);

            if (implied)
            {
                _accumulator = result;
            }
            else
            {
                _bus.WriteByte(addr, result);
            }
            SetFlag(StatusFlags.Carry, ((operand & 0x01) != 0));
            SetNegativeAndZeroFlags(result);
            _master_cycle += cycles;
        }

        public void ROL(ushort addr, byte cycles, bool implied)
        {
            byte carryBit = (byte) (IsFlagSet(StatusFlags.Carry) ? 1 : 0);
            byte operand = implied ? _accumulator : _bus.ReadByte(addr);
            byte result = (byte) ((operand << 1) | carryBit);

            if (implied)
            {
                _accumulator = result;
            }
            else
            {
                _bus.WriteByte(addr, result);
            }
            SetFlag(StatusFlags.Carry, ((operand & 0x80) != 0));
            SetNegativeAndZeroFlags(result);
            _master_cycle += cycles;
        }

        public void ROR(ushort addr, byte cycles, bool implied)
        {
            byte carryBit = (byte)(IsFlagSet(StatusFlags.Carry) ? 1 : 0);
            byte operand = implied ? _accumulator : _bus.ReadByte(addr);
            byte result = (byte)((operand >> 1) | (carryBit << 7));

            if (implied)
            {
                _accumulator = result;
            }
            else
            {
                _bus.WriteByte(addr, result);
            }
            SetFlag(StatusFlags.Carry, ((operand & 0x01) != 0));
            SetNegativeAndZeroFlags(result);
            _master_cycle += cycles;
        }

        public void BIT(ushort addr, byte cycles)
        {
            byte operand = _bus.ReadByte(addr);
            byte result = (byte) (_accumulator & operand);

            SetFlag(StatusFlags.Zero, (result == 0));
            SetFlag(StatusFlags.Negative, ((operand & (1 << 7)) != 0));
            SetFlag(StatusFlags.Overflow, ((operand & (1 << 6)) != 0));

            _master_cycle += cycles;
        }

        public void PHA(byte cycles = 3)
        {
            PushByte(_accumulator);
            _master_cycle += cycles;
        }

        public void PLA(byte cycles = 4)
        {
            byte accumulatorOld = PopByte();
            _accumulator = accumulatorOld;
            SetNegativeAndZeroFlags(_accumulator);
            _master_cycle += cycles;
        }

        public void PHP(byte cycles = 3)
        {
            PushByte((byte)(_status | 0b0011_0000));
            _master_cycle += cycles;
        }

        public void PLP(byte cycles = 4)
        {
            byte statusOld = PopByte();
            _status = (byte)(statusOld & 0b1110_1111);
            _master_cycle += cycles;
        }

        public void RTI(byte cycles = 6)
        {
            byte statusOld = PopByte();
            _status = (byte)(statusOld & 0b1110_1111);
            _program_counter = PopWord();
            _master_cycle += cycles;
        }

        public void BRK()
        {
            PushWord((ushort)(_program_counter + 2));

            Console.WriteLine($"DEBUG:Addr Pushed To Stack {_program_counter}");

            PushByte((byte)(_status | 0b0011_0000));
            SetFlag(StatusFlags.InterruptDisable, true);

            // Read from Interrupt Vector @ FFFE-FFFF
            byte lo = _bus.ReadByte(0xFFFE);
            byte hi = _bus.ReadByte(0xFFFF);

            _program_counter = (ushort)((hi << 8) | lo);
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

        void BEQ_Relative() => BEQ(Addr_Relative(), 2); // F0
        void BNE_Relative() => BNE(Addr_Relative(), 2); // D0
        void BCC_Relative() => BCC(Addr_Relative(), 2); // 90
        void BCS_Relative() => BCS(Addr_Relative(), 2); // B0
        void BPL_Relative() => BPL(Addr_Relative(), 2); // 10
        void BMI_Relative() => BMI(Addr_Relative(), 2); // 30
        void BVC_Relative() => BVC(Addr_Relative(), 2); // 50;
        void BVS_Relative() => BVS(Addr_Relative(), 2); // 70;

        void ADC_Immediate() => ADC(Addr_Immediate(), 2); // 69
        void ADC_ZeroPage() => ADC(Addr_ZeroPage(), 3); // 65 
        void ADC_ZeroPageX() => ADC(Addr_ZeroPageX(), 4); // 75
        void ADC_Absolute() => ADC(Addr_Absolute(), 4); // 6D
        void ADC_AbsoluteX() => ADC(Addr_AbsoluteX(), 4); // 7D
        void ADC_AbsoluteY() => ADC(Addr_AbsoluteY(), 4); // 79
        void ADC_IndirectX() => ADC(Addr_IndirectX(), 6); // 61
        void ADC_IndirectY() => ADC(Addr_IndirectY(), 5); // 71

        void SBC_Immediate() => SBC(Addr_Immediate(), 2); // E9
        void SBC_ZeroPage() => SBC(Addr_ZeroPage(), 3); // E5
        void SBC_ZeroPageX() => SBC(Addr_ZeroPageX(), 4); // F5
        void SBC_Absolute() => SBC(Addr_Absolute(), 4); // ED
        void SBC_AbsoluteX() => SBC(Addr_AbsoluteX(), 4); // FD
        void SBC_AbsoluteY() => SBC(Addr_AbsoluteY(), 4); // F9
        void SBC_IndirectX() => SBC(Addr_IndirectX(), 6); // E1
        void SBC_IndirectY() => SBC(Addr_IndirectY(), 5); // F1

        void CMP_Immediate() => CMP(Addr_Immediate(), 2); // C9
        void CMP_ZeroPage() => CMP(Addr_ZeroPage(), 3); // C5
        void CMP_ZeroPageX() => CMP(Addr_ZeroPageX(), 4); // D5
        void CMP_Absolute() => CMP(Addr_Absolute(), 4); // CD
        void CMP_AbsoluteX() => CMP(Addr_AbsoluteX(), 4); // DD 
        void CMP_AbsoluteY() => CMP(Addr_AbsoluteY(), 4); // D9
        void CMP_IndirectX() => CMP(Addr_IndirectX(), 6); // C1
        void CMP_IndirectY() => CMP(Addr_IndirectY(), 5); // D1

        void CPX_Immediate() => CPX(Addr_Immediate(), 2); // E0
        void CPX_ZeroPage() => CPX(Addr_ZeroPage(), 3); // E4
        void CPX_Absolute() => CPX(Addr_Absolute(), 4); // EC

        void CPY_Immediate() => CPY(Addr_Immediate(), 2); // C0
        void CPY_ZeroPage() => CPY(Addr_ZeroPage(), 3); // C4
        void CPY_Absolute() => CPY(Addr_Absolute(), 4); // CC

        void AND_Immediate() => AND(Addr_Immediate(), 2); // 29
        void AND_ZeroPage() => AND(Addr_ZeroPage(), 3); // 25
        void AND_ZeroPageX() => AND(Addr_ZeroPageX(), 4); // 35
        void AND_Absolute() => AND(Addr_Absolute(), 4); // 2D
        void AND_AbsoluteX() => AND(Addr_AbsoluteX(), 4); // 3D
        void AND_AbsoluteY() => AND(Addr_AbsoluteY(), 4); // 39
        void AND_IndirectX() => AND(Addr_IndirectX(), 6); // 21
        void AND_IndirectY() => AND(Addr_IndirectY(), 5); // 31

        void ORA_Immediate() => ORA(Addr_Immediate(), 2); // 09
        void ORA_ZeroPage() => ORA(Addr_ZeroPage(), 3); // 05
        void ORA_ZeroPageX() => ORA(Addr_ZeroPageX(), 4); // 15
        void ORA_Absolute() => ORA(Addr_Absolute(), 4); // 0D
        void ORA_AbsoluteX() => ORA(Addr_AbsoluteX(), 4); // 1D
        void ORA_AbsoluteY() => ORA(Addr_AbsoluteY(), 4); // 19
        void ORA_IndirectX() => ORA(Addr_IndirectX(), 6); // 01
        void ORA_IndirectY() => ORA(Addr_IndirectY(), 5); // 11

        void EOR_Immediate() => EOR(Addr_Immediate(), 2); // 49
        void EOR_ZeroPage() => EOR(Addr_ZeroPage(), 3); // 45
        void EOR_ZeroPageX() => EOR(Addr_ZeroPageX(), 4); // 55
        void EOR_Absolute() => EOR(Addr_Absolute(), 4); // 4D
        void EOR_AbsoluteX() => EOR(Addr_AbsoluteX(), 4); // 5D
        void EOR_AbsoluteY() => EOR(Addr_AbsoluteY(), 4); // 59
        void EOR_IndirectX() => EOR(Addr_IndirectX(), 6); // 41
        void EOR_IndirectY() => EOR(Addr_IndirectY(), 5); // 51

        void ASL_Implied() => ASL(0x0000, 2, true); // 0A
        void ASL_ZeroPage() => ASL(Addr_ZeroPage(), 5, false); // 06
        void ASL_ZeroPageX() => ASL(Addr_ZeroPageX(), 6, false); // 16
        void ASL_Absolute() => ASL(Addr_Absolute(), 6, false); // 0E
        void ASL_AbsoluteX() => ASL(Addr_AbsoluteX(), 7, false); // 1E

        void LSR_Implied() => LSR(0x0000, 2, true); // 4A
        void LSR_ZeroPage() => LSR(Addr_ZeroPage(), 5, false); // 46
        void LSR_ZeroPageX() => LSR(Addr_ZeroPageX(), 6, false); // 56
        void LSR_Absolute() => LSR(Addr_Absolute(), 6, false); // 4E
        void LSR_AbsoluteX() => LSR(Addr_AbsoluteX(), 7, false); // 5E

        void ROL_Implied() => ROL(0x0000, 2, true); // 2A
        void ROL_ZeroPage() => ROL(Addr_ZeroPage(), 5, false); // 26
        void ROL_ZeroPageX() => ROL(Addr_ZeroPageX(), 6, false); // 36
        void ROL_Absolute() => ROL(Addr_Absolute(), 6, false); // 2E
        void ROL_AbsoluteX() => ROL(Addr_AbsoluteX(), 7, false); // 3E

        void ROR_Implied() => ROR(0x0000, 2, true); // 6A
        void ROR_ZeroPage() => ROR(Addr_ZeroPage(), 5, false); // 66
        void ROR_ZeroPageX() => ROR(Addr_ZeroPageX(), 6, false); // 76
        void ROR_Absolute() => ROR(Addr_Absolute(), 6, false); // 6E
        void ROR_AbsoluteX() => ROR(Addr_AbsoluteX(), 7, false); // 7E

        void RTI_Implied() => RTI(); // 40

        void BIT_ZeroPage() => BIT(Addr_ZeroPage(), 3); // 24
        void BIT_Absolute() => BIT(Addr_Absolute(), 4); // 2C

        void INC_ZeroPage() => INC(Addr_ZeroPage(), 5); // E6
        void INC_ZeroPageX() => INC(Addr_ZeroPageX(), 6); // F6
        void INC_Absolute() => INC(Addr_Absolute(), 6); // EE
        void INC_AbsoluteX() => INC(Addr_AbsoluteX(), 7); // FE

        void DEC_ZeroPage() => DEC(Addr_ZeroPage(), 5); // C6
        void DEC_ZeroPageX() => DEC(Addr_ZeroPageX(), 6); // D6
        void DEC_Absolute() => DEC(Addr_Absolute(), 6); // CE
        void DEC_AbsoluteX() => DEC(Addr_AbsoluteX(), 7); // DE

        #endregion
        #endregion
        #region ##### Execution #####
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

                // Stack Instructions
                case 0x48: PHA(); break;
                case 0x08: PHP(); break;
                case 0x68: PLA(); break;
                case 0x28: PLP(); break;

                // Transfer Instructions
                case 0xAA: TAX(); break;
                case 0x8A: TXA(); break;
                case 0xA8: TAY(); break;
                case 0x98: TYA(); break;
                case 0x9A: TXS(); break;
                case 0xBA: TSX(); break;

                // Arithmetic Instructions
                case 0xE8: INX(); break;
                case 0xCA: DEX(); break;
                case 0xC8: INY(); break;
                case 0x88: DEY(); break;

                case 0xE6: INC_ZeroPage(); break;
                case 0xF6: INC_ZeroPageX(); break;
                case 0xEE: INC_Absolute(); break;
                case 0xFE: INC_AbsoluteX(); break;

                case 0xC6: DEC_ZeroPage(); break;
                case 0xD6: DEC_ZeroPageX(); break;
                case 0xCE: DEC_Absolute(); break;
                case 0xDE: DEC_AbsoluteX(); break;

                case 0x69: ADC_Immediate(); break;
                case 0x65: ADC_ZeroPage(); break;
                case 0x75: ADC_ZeroPageX(); break;
                case 0x6D: ADC_Absolute(); break;
                case 0x7D: ADC_AbsoluteX(); break;
                case 0x79: ADC_AbsoluteY(); break;
                case 0x61: ADC_IndirectX(); break;
                case 0x71: ADC_IndirectY(); break;

                case 0xE9: SBC_Immediate(); break;
                case 0xE5: SBC_ZeroPage(); break;
                case 0xF5: SBC_ZeroPageX(); break;
                case 0xED: SBC_Absolute(); break;
                case 0xFD: SBC_AbsoluteX(); break;
                case 0xF9: SBC_AbsoluteY(); break;
                case 0xE1: SBC_IndirectX(); break;
                case 0xF1: SBC_IndirectY(); break;

                case 0xC9: CMP_Immediate(); break;
                case 0xC5: CMP_ZeroPage(); break;
                case 0xD5: CMP_ZeroPageX(); break;
                case 0xCD: CMP_Absolute(); break;
                case 0xDD: CMP_AbsoluteX(); break;
                case 0xD9: CMP_AbsoluteY(); break;
                case 0xC1: CMP_IndirectX(); break;
                case 0xD1: CMP_IndirectY(); break;

                case 0xE0: CPX_Immediate(); break;
                case 0xE4: CPX_ZeroPage(); break;
                case 0xEC: CPX_Absolute(); break;

                case 0xC0: CPY_Immediate(); break;
                case 0xC4: CPY_ZeroPage(); break;
                case 0xCC: CPY_Absolute(); break;

                // Logical Instructions
                case 0x29: AND_Immediate(); break;
                case 0x25: AND_ZeroPage(); break;
                case 0x35: AND_ZeroPageX(); break;
                case 0x2D: AND_Absolute(); break;
                case 0x3D: AND_AbsoluteX(); break;
                case 0x39: AND_AbsoluteY(); break;
                case 0x21: AND_IndirectX(); break;
                case 0x31: AND_IndirectY(); break;

                case 0x09: ORA_Immediate(); break;
                case 0x05: ORA_ZeroPage(); break;
                case 0x15: ORA_ZeroPageX(); break;
                case 0x0D: ORA_Absolute(); break;
                case 0x1D: ORA_AbsoluteX(); break;
                case 0x19: ORA_AbsoluteY(); break;
                case 0x01: ORA_IndirectX(); break;
                case 0x11: ORA_IndirectY(); break;

                case 0x49: EOR_Immediate(); break;
                case 0x45: EOR_ZeroPage(); break;
                case 0x55: EOR_ZeroPageX(); break;
                case 0x4D: EOR_Absolute(); break;
                case 0x5D: EOR_AbsoluteX(); break;
                case 0x59: EOR_AbsoluteY(); break;
                case 0x41: EOR_IndirectX(); break;
                case 0x51: EOR_IndirectY(); break;

                case 0x0A: ASL_Implied(); break;
                case 0x06: ASL_ZeroPage(); break;
                case 0x16: ASL_ZeroPageX(); break;
                case 0x0E: ASL_Absolute(); break;
                case 0x1E: ASL_AbsoluteX(); break;

                case 0x4A: LSR_Implied(); break;
                case 0x46: LSR_ZeroPage(); break;
                case 0x56: LSR_ZeroPageX(); break;
                case 0x4E: LSR_Absolute(); break;
                case 0x5E: LSR_AbsoluteX(); break;

                case 0x2A: ROL_Implied(); break;
                case 0x26: ROL_ZeroPage(); break;
                case 0x36: ROL_ZeroPageX(); break;
                case 0x2E: ROL_Absolute(); break;
                case 0x3E: ROL_AbsoluteX(); break;

                case 0x6A: ROR_Implied(); break;
                case 0x66: ROR_ZeroPage(); break;
                case 0x76: ROR_ZeroPageX(); break;
                case 0x6E: ROR_Absolute(); break;
                case 0x7E: ROR_AbsoluteX(); break;

                case 0x24: BIT_ZeroPage(); break;
                case 0x2C: BIT_Absolute(); break;

                // Flag Instructions
                case 0x18: CLC(); break;
                case 0x38: SEC(); break;
                case 0x58: CLI(); break;
                case 0x78: SEI(); break;
                case 0xB8: CLV(); break;
                case 0xD8: CLD(); break;
                case 0xF8: SED(); break;

                // Program Control & Branching
                case 0x4C: JMP_Absolute(); break;
                case 0x6C: JMP_Indirect(); break;
                case 0x20: JSR_Absolute(); break;
                case 0x60: RTS(); break;
                case 0x40: RTI_Implied(); break;
                case 0xF0: BEQ_Relative(); break;
                case 0xD0: BNE_Relative(); break;
                case 0x90: BCC_Relative(); break;
                case 0xB0: BCS_Relative(); break;
                case 0x10: BPL_Relative(); break;
                case 0x30: BMI_Relative(); break;
                case 0x50: BVC_Relative(); break;
                case 0x70: BVS_Relative(); break;

                // NOP Variants (more eventually)
                case 0xEA: _master_cycle += 2; break;
                case 0x04: _master_cycle += 3; break;

                default: Console.WriteLine($"No Match For Opcode {Convert.ToString(opcode, 16)}"); break;
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
        #endregion

        public void IRQ()
        {
            if (!IsFlagSet(StatusFlags.InterruptDisable))
            {
                PushWord(_program_counter);

                PushByte((byte)(_status | 0b0011_0000));
                SetFlag(StatusFlags.InterruptDisable, true);

                _program_counter = (ushort)((_bus.ReadByte(0xFFFE) | _bus.ReadByte(0xFFFF) << 8));

                _master_cycle += 7;
            }
        }

        public void NMI()
        {
            PushWord(_program_counter);

            PushByte((byte)(_status | 0b0011_0000));
            SetFlag(StatusFlags.InterruptDisable, true);

            _program_counter = (ushort)((_bus.ReadByte(0xFFFA) | _bus.ReadByte(0xFFFB) << 8));

            _master_cycle += 7;
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
            if (value)
            {
                _status |= (byte)flag;
            }
            else
            {
                _status &= (byte)~flag;
            }
        }

        public void SetNegativeAndZeroFlags(byte value)
        {
            SetFlag(StatusFlags.Zero, value == 0);
            SetFlag(StatusFlags.Negative, (value & 0x80) != 0);
        }

        public void SetOverflowAndCarryFlags(ushort result, byte operand)
        {
            SetFlag(StatusFlags.Carry, result > 255);
            // Two values of the same sign summing to a value of the opposite sign
            bool overflow = ((~(_accumulator ^ operand) & (_accumulator ^ result)) & 0x80) != 0;
            SetFlag(StatusFlags.Overflow, overflow);
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

            _program_counter = (ushort)((hi << 8) | lo);
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
