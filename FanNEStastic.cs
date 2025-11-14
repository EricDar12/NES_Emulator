using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emulator
{
    internal class FanNEStastic
    {
        static readonly ushort NES_WIDTH = 256;
        static readonly ushort NES_HEIGHT = 240;
        static bool _isRunning = false;
        static int _frameCount = 0;

        static float _residualTime = 0.0f;
        static float _elapsedTime = 0.0f;
        static uint _previousFrameTime = SDL.SDL_GetTicks();
        static uint _newFrameTime = 0;


        public static void Run(string ROMFilePath)
        {
            NES_Cartridge _cart = new NES_Cartridge(ROMFilePath);
            NES_System _nes = new NES_System(_cart);

            // TODO: Implement a controller class
            Dictionary<SDL.SDL_Scancode, byte> _keyMap = new Dictionary<SDL.SDL_Scancode, byte> {
                { SDL.SDL_Scancode.SDL_SCANCODE_X, 0x80 },
                { SDL.SDL_Scancode.SDL_SCANCODE_Z, 0x40 },
                { SDL.SDL_Scancode.SDL_SCANCODE_A, 0x20 },
                { SDL.SDL_Scancode.SDL_SCANCODE_S, 0x10 },
                { SDL.SDL_Scancode.SDL_SCANCODE_UP, 0x08 },
                { SDL.SDL_Scancode.SDL_SCANCODE_DOWN, 0x04 },
                { SDL.SDL_Scancode.SDL_SCANCODE_LEFT, 0x02 },
                { SDL.SDL_Scancode.SDL_SCANCODE_RIGHT, 0x01 }
            };

            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_EVENTS) < 0)
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

            IntPtr renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

            SDL.SDL_RenderSetLogicalSize(renderer, NES_WIDTH, NES_HEIGHT);

            IntPtr texture = SDL.SDL_CreateTexture(
                renderer,
                SDL.SDL_PIXELFORMAT_ARGB8888,
                (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                NES_WIDTH,
                NES_HEIGHT
            );

            PlayStartupSequence(renderer);

            // Force a black frame after the startup sequence ends
            SDL.SDL_RenderClear(renderer);
            SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
            SDL.SDL_RenderPresent(renderer);

            SDL.SDL_Event sdlEvent;

            _isRunning = true;
            while (_isRunning)
            {

                _newFrameTime = SDL.SDL_GetTicks();
                _elapsedTime = (_newFrameTime - _previousFrameTime) / 1000f;
                _previousFrameTime = _newFrameTime;

                while (SDL.SDL_PollEvent(out sdlEvent) != 0)
                {
                    if (sdlEvent.type == SDL.SDL_EventType.SDL_QUIT)
                    {
                        _isRunning = false;
                    }
                }

                // Reset controller state on each frame
                // works well but might be worth looking into better solutions
                _nes._bus._controller[0] = 0x00;
                SDL.SDL_PumpEvents();
                
                unsafe
                {
                    byte* keyboardState = (byte*)SDL.SDL_GetKeyboardState(out _);

                    foreach (var button in _keyMap)
                    {
                        if (keyboardState[(int)button.Key] != 0)
                        {
                            _nes._bus._controller[0] |= (byte)button.Value; // Currently only supports one controller
                        }
                    }
                }

                // Run the NES
                if (_residualTime > 0.0f)
                {
                    _residualTime -= _elapsedTime;
                }
                else
                {
                    _residualTime += (1.0f / 60.0f) - _elapsedTime;
                    do { _nes.Clock(); } while (!_nes._ppu._isFrameComplete);
                    _nes._ppu._isFrameComplete = false;
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
                SDL.SDL_Delay(1);
                _frameCount++;
            }
            SDL.SDL_DestroyTexture(texture);
            SDL.SDL_DestroyRenderer(renderer);
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
        }

        private static void PlayStartupSequence(nint renderer)
        {
            IntPtr surf = SDL.SDL_LoadBMP("startup/faNEStastic_logo.bmp");

            if (surf == IntPtr.Zero)
            {
                Console.WriteLine("Failed to Load Startup BMP " + SDL.SDL_GetError());
            }

            IntPtr imageTexture = SDL.SDL_CreateTextureFromSurface(renderer, surf);

            if (imageTexture == IntPtr.Zero)
            {
                Console.WriteLine("Failed to Create Startup Texture " + SDL.SDL_GetError());
            }

            SDL.SDL_FreeSurface(surf);
            SDL.SDL_RenderClear(renderer);

            SDL.SDL_Rect dst = new SDL.SDL_Rect {
                x = 0,
                y = 0,
                w = NES_WIDTH,
                h = NES_HEIGHT
            };

            // Display the startup sequence for 2 seconds
            SDL.SDL_RenderCopy(renderer, imageTexture, IntPtr.Zero, ref dst);
            SDL.SDL_RenderPresent(renderer);
            SDL.SDL_Delay(2000);
            SDL.SDL_DestroyTexture(imageTexture);
        }
    }
}
