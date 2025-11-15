# FanNEStastic 
## An exploration of NES hardware emulation

---
## Project Status
<p align="left">
  <img src="https://img.shields.io/badge/CPU-✅_Complete-brightgreen" />
  <img src="https://img.shields.io/badge/PPU-✅_Complete-brightgreen" />
  <img src="https://img.shields.io/badge/BUS-✅_Complete-brightgreen" />
  <img src="https://img.shields.io/badge/Input-⏳_In%20Progress-yellow" />
  <img src="https://img.shields.io/badge/APU-⏳_Planned-orange" />
  <img src="https://img.shields.io/badge/More Mappers-⏳_In%20Progress-yellow" />
</p>

---
<table align="center">
  <tr>
    <td>
      <img src="https://github.com/user-attachments/assets/a76067e6-5a0c-4d4a-9eb6-25536a06b450" alt="video1">
    </td>
    <td>
      <img src="https://github.com/user-attachments/assets/59e69c79-e42c-4189-9b06-778f0faf790e" alt="video2">
    </td>
  </tr>
</table>
---

## Project Goals 
- Deepen understanding of hardware emulation, computer architecture, and CPU design  
- Implement the Ricoh 6502 CPU with instruction level accuracy and complete instruction set coverage
- Study and recreate the NES PPU, including accurate background and sprite rendering  
- Support multiple cartridge mappers to correctly handle ROM banking and memory layout  
- Implement accurate memory operations, including NES memory mirroring  
- Build a basic frontend/GUI
- Implement save states and other fun peripherial features
- Build a working emulator capable of running classic NES games reliably

---

## Documentation
Detailed explanations for each component, as well as how I overcame significant engineering challenges can be found below:
- [CPU](./docs/The-6502-CPU.md)    
- BUS, APU & Input coming soon...  

---
<table align="center">
  <tr>
    <td>
      <img src="https://github.com/user-attachments/assets/9220f716-3077-4f98-9d2d-7daabd6505d4" alt="video2">
    </td>
    <td>
      <img src="https://github.com/user-attachments/assets/3ad41ee6-e7d7-4715-9025-1876e5829bac" alt="video2">
    </td>
  </tr>
</table>

---

## Acknowledgment

Portions of this emulator were adapted from the excellent *"NES Emulator From Scratch"* tutorial series by **David Barr (Javidx9 / OneLoneCoder)**.  
His work provided valuable insight into NES hardware architecture and PPU design.
Used under the terms of the **OLC-3 License**.  

---
