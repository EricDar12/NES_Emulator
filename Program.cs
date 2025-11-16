using System;
using SDL2;

namespace NES_Emulator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // TODO: File selctor for ROM loading, oot hardcoded paths
            FanNEStastic.Run("C:\\Users\\eric1\\Documents\\Projects\\Test ROMs\\gal.nes");
        }
    }
}