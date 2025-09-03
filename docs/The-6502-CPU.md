# The Ricoh 6502 CPU
##### Or more accurately, the Ricoh 2A03 (NTSC) and 2A07 (PAL)
---

## Synopsis
The Ricoh 2A03/2A07 is an 8-bit microprocessor at the heart of the NES. It is a modified version of the popular MOS 6502 CPU, featuring integrated audio capabilities and minor hardware differences for the NTSC and PAL systems. This section explores the CPU’s architecture, instruction set, and cycle-accurate behavior in the context of emulation.

The NTSC & PAL chips ran at slightly different clock speeds, but were otherwise **functionally identical**. As such, I will simply refer to the chip as the 2A03 moving forward.

---

## Key Details
- Based on the widely popular 8-bit MOS Technology 6502 CPU
- Ran at 1.79 MHz (NTSC) or 1.66 MHz (PAL)
- Instruction set consists of 56 official instructions
- Decimal mode disabled

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

We can detect page crossing with a simple check where we compare the current memory address, and the new memory address after indexing:

`public void IsPageCrossed(ushort baseAddr, ushort finalAddr)
    {
        if ((baseAddr & 0xFF00) != (finalAddr & 0xFF00))
        {
            _master_cycle += 1;
        }
    }
`

---

## Instruction Set
A 16x16 table contains all of the instructions used by the 2A03. While all 256 spaces **are** used, only 113 are considered official, the remaining 143 are known as **illegal opcodes**, rarely utilized by the NES. They will not be implemented in my emulator.

TODO

---


