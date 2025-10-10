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
            NES_Cartridge _cart = new NES_Cartridge("C:\\Users\\eric1\\OneDrive\\Documents\\NES_Emulator\\Test ROMs\\dk.nes");
            //NES_Cartridge _cart = new NES_Cartridge("C:\\Users\\eric1\\Documents\\Visual Studio 2022\\Projects\\Test ROMs\\smb.nes");
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
                int cycleCount = 0;
                do
                {
                    _nes.Clock();
                    cycleCount++;
                } while (!_nes._ppu._isFrameComplete);

                frameCount++;
                Console.WriteLine($"Frame {frameCount} complete. Cycles: {cycleCount}, Scanline: {_nes._ppu._scanline}, Cycle: {_nes._ppu._cycles}");

                _nes._ppu._isFrameComplete = false;

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
            //_nes._cpu.LogProcessorStatus();
        }
    }
}

