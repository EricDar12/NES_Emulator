# The Ricoh 6502 CPU
##### Or more accurately, the Ricoh 2A03 (NTSC) and 2A07 (PAL)
---

## Synopsis
The Ricoh 2A03/2A07 is an 8-bit microprocessor at the heart of the NES. It is a modified version of the popular MOS 6502 CPU, featuring integrated audio capabilities and minor hardware differences for the NTSC and PAL systems. This section explores the CPU’s architecture, instruction set, and cycle-accurate behavior in the context of emulation.

The NTSC & PAL chips ran at slightly different clock speeds, but were otherwise **functionally identical**.

---

## Key Details
- Based on the 8-bit MOS Technology 6502 CPU
- Ran at 1.79 MHz (NTSC) or 1.66 MHz (PAL)
- Instruction set consists of 56 official instructions
- Decimal mode disabled

--- 

## 6502 Architecture

##### Registers:
- Accumulator (A)
- X Register (X)
- Y Register (Y)
- Status Register (SR)
- Stack Pointer (SP)
- Program Counter (PC)

##### Program Counter:
- The only 16-bit register on the 6502
- Holds the 16-bit address of the next instruction to be executed

##### Status Register:
- Specialized register that holds status flags
- Individual bits indicate the result of the most recent operation

We can conveniently emulate this register with an enum

```        
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
```

And use methods to check and set different states depending on the result of a particular instruction

```
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
```
##### Stack:
- Exists in the first page of memory (0100 - 01FF)
- Grows downwards (descending stack)
- SP decrements on push, and increments on pop
- SP acts as an offset within this memory region

---

## Addressing Modes

The Ricoh 2A03 supports 13 addressing modes. These modes determine how the CPU finds the operand for an instruction.

#### Immediate Mode
- The value immediately following the instruction is used as the operand.  
- **Example:** `LDA #$0A` (Load the A register with `0x0A`)

#### Zero Page Mode
- Operand is in the **first 256 bytes of memory** (`0x0000` - `0x00FF`).  
- The argument acts as an offset within this page, making it **fast**.  
- **Example:** `LDA $42` (Load A from `0x0042`)

#### Absolute Mode
- Instruction includes a **16-bit memory address** pointing directly to the operand.  
- Accesses the full 64KB memory space.  
- **Example:** `LDA $1234` (Load A from `0x1234`)

#### Relative Mode
- Used for **branch instructions**.  
- Argument is a **signed 8-bit offset**, allowing jumps forwards or backwards relative to the program counter.  
- **Example:** `BEQ label` (Branch if zero flag set)

#### Indirect Mode
- Uses a **pointer stored in memory**. Instruction provides a 16-bit address, which contains the actual operand address.  
- Only used by `JMP`.  
- **Example:** `JMP ($3000)` — jump to address stored at `0x3000`

#### Implied / Accumulator Mode
- No operand is required; instruction operates directly on a register or CPU state.  
- **Examples:** `CLC`, `INX`, `ASL A`

---

### Indexed Addressing Modes

These modes are variations of Zero Page or Absolute addressing where an **index register** offsets the base address:

- **Zero Page,X / Zero Page,Y** Adds X or Y to a zero-page address (wraps around 0x00–0xFF)  
- **Absolute,X / Absolute,Y**  Adds X or Y to a 16-bit address (may cross page boundary)  
- **Indexed Indirect (Indirect,X)** X offsets a zero-page pointer, then CPU reads 16-bit pointer for final address  
- **Indirect Indexed (Indirect),Y** CPU reads zero-page pointer, then adds Y for final address  

---

### Addressing Mode Quirks

- Each addressing mode requires a different amount of CPU clock cycles
- Indexed modes may require additional cycles if they **cross page boundaries** (01FF + 1 = 0200) The high byte represents the page of memory we are within. 

We can detect page crossing by comparing the high byte of both the base memory address with the indexed, final memory address.

```
if ((baseAddr & 0xFF00) != (finalAddr & 0xFF00))
{
    add one cycle
}
```
---

## Instruction Set

The 2A03’s instruction set can be represented as a 16×16 table of 256 opcodes. Of these, 151 are **official instructions**. The remaining 105 opcodes are **unofficial instructions**. While some NES games make use of these unofficial instructions, they are much less common and will not be implemented in this emulator until much later.

Some basic opcodes include:
- LDA (Load Accumulator)
- STA (Store Accumulator in memory)
- ADC (Add with carry)
- SBC (Subtract with carry)

Each opcode is represented by a byte **corresponding to its position in this table**. For example, when the CPU decodes the byte 0xA9, the LDA instruction will be executed.

The table can be viewed here: https://www.masswerk.at/6502/6502_instruction_set.html

---

## Execution
When I began this project, I started with a simple goal in mind, **make it work**. Avoiding over-engineering was a top priority. While many emulators implement instruction classes, addressing mode enums, and complex tables storing all of this information, I opted for a simple switch case.

```
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
        //etc...
    }
}
```
Despite the large size of this switch case, **it remains performant**. In fact, because `opcode` is a small integer type, the C# compiler is able to  generate a **jump table**, enabling constant time lookup for all opcodes.

Finally, with the addition of a single step feature, we are able to execute CPU instructions **one at a time**, and observe the internal CPU state. This is invaluable for debugging, specifically for **test ROMs**. These are ROMs specifically designed to test the accuracy of your CPU emulation. The most famous of which being nestest.nes by **Kevtris**. This emulator successfully passes all tests in this test ROM, a huge milestone.

---

## Notes & Takeaways

This document serves as a high-level overview of the 6502 CPU. Covering everything in detail would require thousands more words. I'm not sure how many of you will have even read *this* far. 

##### One thing is for certain:

Emulating a CPU is **extraordinarily complex**, even one considered simple by today's standards (and even in the 1980s, for that matter). Completing this component is a huge acomplishment, but it is still only the first step in a **much** larger project.

---





        





