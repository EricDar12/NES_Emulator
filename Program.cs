using System;
using SDL2;

namespace NES_Emulator
{
    internal class Program
    {
        static readonly ushort NES_WIDTH = 256;
        static readonly ushort NES_HEIGHT = 240;

        static bool _isRunning = false;
        static byte _selectedPalette = 0x00;

        static void Main(string[] args)
        {

            NES_Cartridge _cart = new NES_Cartridge("C:\\Users\\eric1\\OneDrive\\Documents\\NES_Emulator\\Test ROMs\\dk.nes");
            NES_System _nes = new NES_System(_cart);
            _nes._ppu.InitializeDefaultPalettes();

            //_nes._cpu._program_counter = 0xC000;
            //_nes._cpu.StepOneInstruction();
            //_nes._cpu.FetchAndDecode();
            //Console.WriteLine("RESULTS: " + _nes._cpu._bus.CPU_Read(0x0002) + " " + _nes._cpu._bus.CPU_Read(0x0003));
            //_nes._cpu.LogProcessorStatus();

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
                128
            );

            _isRunning = true;

            while (_isRunning)
            {
                //_nes._cpu._program_counter = 0xC000;
                do { _nes.Clock(); } while (!_nes._ppu._isFrameComplete);
                _nes._ppu._isFrameComplete = false;

                SDL.SDL_Event e;
                while (SDL.SDL_PollEvent(out e) != 0)
                {
                    if (e.type == SDL.SDL_EventType.SDL_QUIT)
                    {
                        _isRunning = false;
                    }
                    // For demonstration purposes, switch active palette
                    if (e.type == SDL.SDL_EventType.SDL_KEYDOWN)
                    {
                        switch (e.key.keysym.sym)
                        {
                            case SDL.SDL_Keycode.SDLK_SPACE:
                                _selectedPalette = (byte)((_selectedPalette + 1) % 8);
                                break;
                        }
                    }
                }


                uint[] left = _nes._ppu.GetPatternTable(0, _selectedPalette);
                uint[] right = _nes._ppu.GetPatternTable(1, _selectedPalette);

                uint[] combined = new uint[256 * 128];

                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        combined[y * 256 + x] = left[y * 128 + x];
                        combined[y * 256 + 128 + x] = right[y * 128 + x];
                    }
                }

                unsafe
                {
                    fixed (uint* ptr = combined)
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

            _nes._cpu.LogProcessorStatus();

        }
    }
}

