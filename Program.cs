using System;
using SDL2;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NES_Emulator
{
    internal class Program
    {
        static readonly ushort NES_WIDTH = 256;
        static readonly ushort NES_HEIGHT = 240;

        static bool _isRunning = true;
        static byte _selectedPalette = 0x00;

        static void Main(string[] args)
        {

            // Different paths for both devices I work on
            //NES_Cartridge _cart = new NES_Cartridge("C:\\Users\\eric1\\OneDrive\\Documents\\NES_Emulator\\Test ROMs\\dk.nes");
            NES_Cartridge _cart = new NES_Cartridge("C:\\Users\\eric1\\Documents\\Visual Studio 2022\\Projects\\Test ROMs\\smb.nes");
            NES_System _nes = new NES_System(_cart);

            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
            {
                Console.WriteLine("Failed To Initialize SDL2 " + SDL.SDL_GetError());
                return;
            }

            IntPtr window = SDL.SDL_CreateWindow(
                title: "FanNEStastic",
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                NES_WIDTH * 3,
                NES_HEIGHT * 3,
                SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN
            );

            if (window == IntPtr.Zero)
            {
                Console.WriteLine("Failed To Create SDL2 Window " + SDL.SDL_GetError());
                SDL.SDL_Quit();
                return;
            }

            IntPtr renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

            SDL.SDL_RenderSetLogicalSize(renderer, NES_WIDTH, NES_HEIGHT);

            IntPtr texture = SDL.SDL_CreateTexture(
                renderer,
                SDL.SDL_PIXELFORMAT_ARGB8888,
                (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                NES_WIDTH,
                NES_HEIGHT
            );

            int frameCount = 0;
            SDL.SDL_Event sdlEvent;
            while (_isRunning)
            {

                while (SDL.SDL_PollEvent(out sdlEvent) != 0)
                {
                    if (sdlEvent.type == SDL.SDL_EventType.SDL_QUIT)
                    {
                        _isRunning = false;
                    }
                }

                do { _nes.Clock(); } while (!_nes._ppu._isFrameComplete);

                unsafe
                {
                    fixed (uint* ptr = _nes._ppu._frameBuffer)
                    {
                        SDL.SDL_UpdateTexture(texture, IntPtr.Zero, (IntPtr)ptr, 256 * sizeof(uint));
                    }
                }

                _nes._ppu._isFrameComplete = false;
                //_nes._bus._controller[0] |= 0xFF; // Simulate buttons being pressed every frame
                //_nes._cpu.LogProcessorStatus();

                //Console.WriteLine($"Frame: {frameCount} ");

                frameCount++;
                //if (frameCount > 30)
                //{
                //    Console.WriteLine("\nNametable 0 contents (rows 0-7):");
                //    for (int row = 0; row < 8; row++)
                //    {
                //        Console.Write($"Row {row}: ");
                //        for (int col = 0; col < 32; col++)
                //        {
                //            Console.Write($"{_nes._ppu._tblName[0, row * 32 + col]:X2} ");
                //        }
                //        Console.WriteLine();
                //    }
                //}

                //if (frameCount % 8 == 0)
                //{
                //    Console.WriteLine($"TileID={_nes._ppu._bgNextTileID:X2} FineY={_nes._ppu._vram.FineY} Attrib={_nes._ppu._bgNextTileAttrib:X2} LSB={_nes._ppu._bgNextTileLSB:X2} MSB={_nes._ppu._bgNextTileMSB:X2}");
                //}

                //_nes._ppu.SetFlag<NES_PPU.PPUMASK>(NES_PPU.PPUMASK.RENDER_BG, ref _nes._ppu._ppuMask, true);

                //if (frameCount % 10 == 0)
                //{
                //    //Console.WriteLine(Convert.ToString(_nes._ppu._ppuMask, 2).PadLeft(8, '0'));
                //}

                //if (_nes._ppu.IsSet<NES_PPU.PPUMASK>(NES_PPU.PPUMASK.RENDER_BG, _nes._ppu._ppuMask))
                //{
                //    Console.WriteLine("Rendering Enabled At Frame: " + frameCount);
                //}
                //else
                //{
                //    Console.WriteLine("Rendering Disabled At Frame: " + frameCount);
                //}

                //if (frameCount >= 30 && frameCount <= 40)
                //{
                //    Console.WriteLine("Palette memory:");
                //    for (int i = 0; i < 32; i++)
                //    {
                //        Console.Write($"[{i:X2}]={_nes._ppu._tblPalette[i]:X2} ");
                //        if ((i + 1) % 8 == 0) Console.WriteLine();
                //    }
                //}
                //if (frameCount % 3 == 0)
                //{
                //    Console.WriteLine($"TRAM: {_nes._ppu._tram._reg:b16} CX:{_nes._ppu._tram.CoarseX:b5} CY:{_nes._ppu._tram.CoarseY:b5} NTX:{_nes._ppu._tram.NameTableX:b1} NTY:{_nes._ppu._tram.NameTableY:b1} FY:{_nes._ppu._tram.FineY:b3} FX:{_nes._ppu._fineX:b3}");
                //    Console.WriteLine($"VRAM: {_nes._ppu._vram._reg:b16} CX:{_nes._ppu._vram.CoarseX:b5} CY:{_nes._ppu._vram.CoarseY:b5} NTX:{_nes._ppu._vram.NameTableX:b1} NTY:{_nes._ppu._vram.NameTableY:b1} FY:{_nes._ppu._vram.FineY:b3} FX:{_nes._ppu._fineX:b3}");
                //}

                SDL.SDL_RenderClear(renderer);
                SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero);
                SDL.SDL_RenderPresent(renderer);
                SDL.SDL_Delay(16);
            }
            SDL.SDL_DestroyTexture(texture);
            SDL.SDL_DestroyRenderer(renderer);
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
        }
    }
}

