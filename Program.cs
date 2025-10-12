using System;
using SDL2;

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

            IntPtr texture = SDL.SDL_CreateTexture(
                renderer,
                SDL.SDL_PIXELFORMAT_ARGB8888,
                (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                256,
                240
            );

            int frameCount = 0;
            while (_isRunning)
            {
                do { _nes.Clock(); } while (!_nes._ppu._isFrameComplete);

                _nes._ppu._isFrameComplete = false;
                _nes._bus._controller[0] |= 0x08;
                _nes._cpu.LogProcessorStatus();

                Thread.Sleep(100);
                //_nes._ppu.SetFlag<NES_PPU.PPUMASK>(NES_PPU.PPUMASK.RENDER_BG, ref _nes._ppu._ppuMask, true);

                frameCount++;
                //Console.WriteLine("\nNametable 0 contents (rows 0-7):");
                //for (int row = 0; row < 8; row++)
                //{
                //    Console.Write($"Row {row}: ");
                //    for (int col = 0; col < 32; col++)
                //    {
                //        Console.Write($"{_nes._ppu._tblName[0, row * 32 + col]:X2} ");
                //    }
                //    Console.WriteLine();
                //}

                if (_nes._ppu.IsSet<NES_PPU.PPUMASK>(NES_PPU.PPUMASK.RENDER_BG, _nes._ppu._ppuMask))
                {
                    Console.WriteLine("Rendering Enabled: " + frameCount);
                }

                unsafe
                {
                    fixed (uint* ptr = _nes._ppu._frameBuffer)
                    {
                        SDL.SDL_UpdateTexture(texture, IntPtr.Zero, (IntPtr)ptr, 256 * sizeof(uint));
                    }
                }

                SDL.SDL_RenderClear(renderer);
                SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero);
                SDL.SDL_RenderPresent(renderer);
                SDL.SDL_Delay(16);
            }
            SDL.SDL_DestroyRenderer(renderer);
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
        }
    }
}

